using MySqlConnector;
using PureWave.Admin.Models;

namespace PureWave.Admin.Services;

public sealed class AdminRepository(MySqlSettings settings)
{
    private readonly string connectionString = settings.ConnectionString;

    public async Task<DashboardSnapshot> GetDashboardAsync(DateOnly? from = null, DateOnly? to = null)
    {
        var fromDate = from ?? DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
        var toDate = to ?? DateOnly.FromDateTime(DateTime.Today);

        await using var connection = OpenConnection();
        await connection.OpenAsync();
        const string sql = """
            select
                (select count(*) from clients),
                (select count(*) from intake_submissions),
                (select count(*) from projects where status = 'Active'),
                coalesce((select sum(i.total_due - coalesce(p.amount_paid, 0))
                          from invoices i
                          left join (
                              select invoice_id, sum(amount) as amount_paid
                              from invoice_payments
                              group by invoice_id
                          ) p on p.invoice_id = i.id), 0),
                coalesce((select sum(amount) from invoice_payments where payment_date between @from_date and @to_date), 0),
                coalesce((select sum(cost_amount * quantity) from project_items where incurred_on between @from_date and @to_date), 0);
            """;
        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@from_date", AsDateTime(fromDate));
        cmd.Parameters.AddWithValue("@to_date", AsDateTime(toDate));
        await using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        return new DashboardSnapshot
        {
            ClientCount = reader.GetInt32(0),
            IntakeCount = reader.GetInt32(1),
            ActiveProjectCount = reader.GetInt32(2),
            OutstandingAmount = reader.GetDecimal(3),
            IncomeForPeriod = reader.GetDecimal(4),
            ExpensesForPeriod = reader.GetDecimal(5)
        };
    }

    public async Task<IReadOnlyList<ClientRecord>> GetClientsAsync()
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync();
        await using var cmd = new MySqlCommand("""
            select id, full_name, email_address, phone_number, address_line1, address_line2, suburb, city, postal_code, notes
            from clients
            order by full_name;
            """, connection);
        await using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<ClientRecord>();
        while (await reader.ReadAsync())
        {
            list.Add(new ClientRecord
            {
                Id = reader.GetInt64(0),
                FullName = reader.GetString(1),
                EmailAddress = reader.GetString(2),
                PhoneNumber = reader.GetString(3),
                AddressLine1 = reader.GetString(4),
                AddressLine2 = reader.GetString(5),
                Suburb = reader.GetString(6),
                City = reader.GetString(7),
                PostalCode = reader.GetString(8),
                Notes = reader.GetString(9)
            });
        }
        return list;
    }

    public async Task<long> SaveClientAsync(ClientRecord client)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync();
        if (client.Id == 0)
        {
            await using var insert = new MySqlCommand("""
                insert into clients (full_name, email_address, phone_number, address_line1, address_line2, suburb, city, postal_code, notes, updated_at)
                values (@full_name, @email_address, @phone_number, @address_line1, @address_line2, @suburb, @city, @postal_code, @notes, utc_timestamp(6));
                """, connection);
            AddClientParams(insert, client);
            await insert.ExecuteNonQueryAsync();
            return insert.LastInsertedId;
        }

        await using var update = new MySqlCommand("""
            update clients
            set full_name = @full_name,
                email_address = @email_address,
                phone_number = @phone_number,
                address_line1 = @address_line1,
                address_line2 = @address_line2,
                suburb = @suburb,
                city = @city,
                postal_code = @postal_code,
                notes = @notes,
                updated_at = utc_timestamp(6)
            where id = @id;
            """, connection);
        AddClientParams(update, client);
        update.Parameters.AddWithValue("@id", client.Id);
        await update.ExecuteNonQueryAsync();
        return client.Id;
    }

    public async Task DeleteClientAsync(long clientId)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync();
        await using var cmd = new MySqlCommand("delete from clients where id = @id;", connection);
        cmd.Parameters.AddWithValue("@id", clientId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<IReadOnlyList<IntakeRecord>> GetIntakesAsync()
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync();
        await using var cmd = new MySqlCommand("""
            select id, submitted_at, full_name, email_address, phone_number, suburb_or_area, interested_plan, budget_band, project_stage, primary_goals, client_id
            from intake_submissions
            order by submitted_at desc;
            """, connection);
        await using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<IntakeRecord>();
        while (await reader.ReadAsync())
        {
            var submittedAtUtc = DateTime.SpecifyKind(reader.GetDateTime(1), DateTimeKind.Utc);
            list.Add(new IntakeRecord
            {
                Id = reader.GetInt64(0),
                SubmittedAt = new DateTimeOffset(submittedAtUtc).ToLocalTime(),
                FullName = reader.GetString(2),
                EmailAddress = reader.GetString(3),
                PhoneNumber = reader.GetString(4),
                SuburbOrArea = reader.GetString(5),
                InterestedPlan = reader.GetString(6),
                BudgetBand = reader.GetString(7),
                ProjectStage = reader.GetString(8),
                PrimaryGoals = reader.GetString(9),
                ClientId = reader.IsDBNull(10) ? null : reader.GetInt64(10)
            });
        }
        return list;
    }

    public async Task<long> CreateClientFromIntakeAsync(long intakeId)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync();
        await using var tx = await connection.BeginTransactionAsync();

        await using var getIntake = new MySqlCommand("""
            select full_name, email_address, phone_number, suburb_or_area, client_id
            from intake_submissions
            where id = @id;
            """, connection, tx);
        getIntake.Parameters.AddWithValue("@id", intakeId);
        await using var reader = await getIntake.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            throw new InvalidOperationException("The intake was not found.");
        }

        if (!reader.IsDBNull(4))
        {
            return reader.GetInt64(4);
        }

        var client = new ClientRecord
        {
            FullName = reader.GetString(0),
            EmailAddress = reader.GetString(1),
            PhoneNumber = reader.GetString(2),
            AddressLine1 = reader.GetString(3)
        };
        await reader.CloseAsync();

        await using var insert = new MySqlCommand("""
            insert into clients (full_name, email_address, phone_number, address_line1, notes, updated_at)
            values (@full_name, @email_address, @phone_number, @address_line1, @notes, utc_timestamp(6));
            """, connection, tx);
        insert.Parameters.AddWithValue("@full_name", client.FullName);
        insert.Parameters.AddWithValue("@email_address", client.EmailAddress);
        insert.Parameters.AddWithValue("@phone_number", client.PhoneNumber);
        insert.Parameters.AddWithValue("@address_line1", client.AddressLine1);
        insert.Parameters.AddWithValue("@notes", string.Empty);
        await insert.ExecuteNonQueryAsync();
        var clientId = insert.LastInsertedId;

        await using var link = new MySqlCommand("update intake_submissions set client_id = @client_id where id = @id;", connection, tx);
        link.Parameters.AddWithValue("@client_id", clientId);
        link.Parameters.AddWithValue("@id", intakeId);
        await link.ExecuteNonQueryAsync();
        await tx.CommitAsync();
        return clientId;
    }

    public async Task<IReadOnlyList<ProjectListItem>> GetProjectsAsync(string? status = null, long? clientId = null)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync();
        await using var cmd = new MySqlCommand("""
            with invoice_summary as (
                select
                    i.project_id,
                    count(*) as invoice_count,
                    coalesce(sum(i.total_due), 0) as total_due,
                    coalesce(sum(pay.amount_paid), 0) as total_paid,
                    max(i.id) as latest_invoice_id
                from invoices i
                left join (
                    select invoice_id, sum(amount) as amount_paid
                    from invoice_payments
                    group by invoice_id
                ) pay on pay.invoice_id = i.id
                group by i.project_id
            )
            select p.id,
                   p.name,
                   c.full_name,
                   case
                       when p.status = 'Cancelled' then 'Cancelled'
                       when coalesce(inv.invoice_count, 0) > 0 and coalesce(inv.total_paid, 0) >= coalesce(inv.total_due, 0) then 'Paid'
                       when coalesce(inv.invoice_count, 0) > 0 then 'Invoiced'
                       else p.status
                   end as project_status,
                   coalesce(sum(pi.cost_amount * pi.quantity), 0),
                   coalesce(sum(pi.billable_amount * pi.quantity), 0)
            from projects p
            join clients c on c.id = p.client_id
            left join project_items pi on pi.project_id = p.id
            left join invoice_summary inv on inv.project_id = p.id
            where (@client_id = 0 or p.client_id = @client_id)
              and (
                  @status = '' or
                  case
                      when p.status = 'Cancelled' then 'Cancelled'
                      when coalesce(inv.invoice_count, 0) > 0 and coalesce(inv.total_paid, 0) >= coalesce(inv.total_due, 0) then 'Paid'
                      when coalesce(inv.invoice_count, 0) > 0 then 'Invoiced'
                      else p.status
                  end = @status
              )
            group by p.id, p.name, c.full_name, p.status, inv.invoice_count, inv.total_due, inv.total_paid
            order by p.id desc;
            """, connection);
        cmd.Parameters.AddWithValue("@status", status ?? string.Empty);
        cmd.Parameters.AddWithValue("@client_id", clientId ?? 0L);
        await using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<ProjectListItem>();
        while (await reader.ReadAsync())
        {
            list.Add(new ProjectListItem
            {
                Id = reader.GetInt64(0),
                Name = reader.GetString(1),
                ClientName = reader.GetString(2),
                Status = reader.GetString(3),
                TotalCost = reader.GetDecimal(4),
                TotalBillable = reader.GetDecimal(5)
            });
        }
        return list;
    }

    public async Task<ProjectRecord?> GetProjectAsync(long projectId)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync();
        await using var cmd = new MySqlCommand("""
            with invoice_summary as (
                select
                    i.project_id,
                    count(*) as invoice_count,
                    coalesce(sum(i.total_due), 0) as total_due,
                    coalesce(sum(pay.amount_paid), 0) as total_paid,
                    max(i.id) as latest_invoice_id
                from invoices i
                left join (
                    select invoice_id, sum(amount) as amount_paid
                    from invoice_payments
                    group by invoice_id
                ) pay on pay.invoice_id = i.id
                group by i.project_id
            )
            select p.id,
                   p.client_id,
                   p.name,
                   p.description,
                   case
                       when p.status = 'Cancelled' then 'Cancelled'
                       when coalesce(inv.invoice_count, 0) > 0 and coalesce(inv.total_paid, 0) >= coalesce(inv.total_due, 0) then 'Paid'
                       when coalesce(inv.invoice_count, 0) > 0 then 'Invoiced'
                       else p.status
                   end,
                   p.start_date,
                   p.due_date,
                   coalesce(inv.invoice_count, 0),
                   coalesce(inv.total_due, 0),
                   coalesce(inv.total_paid, 0),
                   coalesce(inv.total_due, 0) - coalesce(inv.total_paid, 0),
                   latest.id,
                   latest.invoice_number,
                   latest.status
            from projects p
            left join invoice_summary inv on inv.project_id = p.id
            left join invoices latest on latest.id = inv.latest_invoice_id
            where p.id = @id;
            """, connection);
        cmd.Parameters.AddWithValue("@id", projectId);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new ProjectRecord
        {
            Id = reader.GetInt64(0),
            ClientId = reader.GetInt64(1),
            Name = reader.GetString(2),
            Description = reader.GetString(3),
            Status = reader.GetString(4),
            StartDate = reader.IsDBNull(5) ? null : DateOnly.FromDateTime(reader.GetDateTime(5)),
            DueDate = reader.IsDBNull(6) ? null : DateOnly.FromDateTime(reader.GetDateTime(6)),
            InvoiceCount = reader.GetInt32(7),
            TotalInvoiced = reader.GetDecimal(8),
            AmountPaid = reader.GetDecimal(9),
            OutstandingAmount = reader.GetDecimal(10),
            LatestInvoiceId = reader.IsDBNull(11) ? null : reader.GetInt64(11),
            LatestInvoiceNumber = reader.IsDBNull(12) ? string.Empty : reader.GetString(12),
            LatestInvoiceStatus = reader.IsDBNull(13) ? string.Empty : reader.GetString(13)
        };
    }

    public async Task<long> SaveProjectAsync(ProjectRecord project)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync();
        if (project.Id == 0)
        {
            await using var insert = new MySqlCommand("""
                insert into projects (client_id, name, description, status, start_date, due_date, updated_at)
                values (@client_id, @name, @description, @status, @start_date, @due_date, utc_timestamp(6));
                """, connection);
            AddProjectParams(insert, project);
            await insert.ExecuteNonQueryAsync();
            return insert.LastInsertedId;
        }

        await using var update = new MySqlCommand("""
            update projects
            set client_id = @client_id,
                name = @name,
                description = @description,
                status = @status,
                start_date = @start_date,
                due_date = @due_date,
                updated_at = utc_timestamp(6)
            where id = @id;
            """, connection);
        AddProjectParams(update, project);
        update.Parameters.AddWithValue("@id", project.Id);
        await update.ExecuteNonQueryAsync();
        return project.Id;
    }

    public async Task<IReadOnlyList<ProjectItemRecord>> GetProjectItemsAsync(long projectId)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync();
        await using var cmd = new MySqlCommand("""
            select id, project_id, item_category, description, quantity, unit_label, cost_amount, billable_amount, incurred_on, notes
            from project_items
            where project_id = @project_id
            order by incurred_on, id;
            """, connection);
        cmd.Parameters.AddWithValue("@project_id", projectId);
        await using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<ProjectItemRecord>();
        while (await reader.ReadAsync())
        {
            list.Add(new ProjectItemRecord
            {
                Id = reader.GetInt64(0),
                ProjectId = reader.GetInt64(1),
                ItemCategory = reader.GetString(2),
                Description = reader.GetString(3),
                Quantity = reader.GetDecimal(4),
                UnitLabel = reader.GetString(5),
                CostAmount = reader.GetDecimal(6),
                BillableAmount = reader.GetDecimal(7),
                IncurredOn = DateOnly.FromDateTime(reader.GetDateTime(8)),
                Notes = reader.GetString(9)
            });
        }
        return list;
    }

    public async Task<long> SaveProjectItemAsync(ProjectItemRecord item)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync();
        if (item.Id == 0)
        {
            await using var insert = new MySqlCommand("""
                insert into project_items (project_id, item_category, description, quantity, unit_label, cost_amount, billable_amount, incurred_on, notes)
                values (@project_id, @item_category, @description, @quantity, @unit_label, @cost_amount, @billable_amount, @incurred_on, @notes);
                """, connection);
            AddItemParams(insert, item);
            await insert.ExecuteNonQueryAsync();
            return insert.LastInsertedId;
        }

        await using var update = new MySqlCommand("""
            update project_items
            set item_category = @item_category,
                description = @description,
                quantity = @quantity,
                unit_label = @unit_label,
                cost_amount = @cost_amount,
                billable_amount = @billable_amount,
                incurred_on = @incurred_on,
                notes = @notes
            where id = @id;
            """, connection);
        AddItemParams(update, item);
        update.Parameters.AddWithValue("@id", item.Id);
        await update.ExecuteNonQueryAsync();
        return item.Id;
    }

    public async Task DeleteProjectItemAsync(long itemId)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync();
        await using var cmd = new MySqlCommand("delete from project_items where id = @id;", connection);
        cmd.Parameters.AddWithValue("@id", itemId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<long> CreateInvoiceFromProjectAsync(long projectId)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync();
        await using var tx = await connection.BeginTransactionAsync();

        await using var header = new MySqlCommand("select client_id from projects where id = @project_id;", connection, tx);
        header.Parameters.AddWithValue("@project_id", projectId);
        var clientId = (long?)await header.ExecuteScalarAsync();
        if (!clientId.HasValue)
        {
            throw new InvalidOperationException("Project not found.");
        }

        var invoiceDate = DateOnly.FromDateTime(DateTime.Today);
        var invoiceNumber = await GenerateInvoiceNumberAsync(connection, tx, invoiceDate);

        await using var insert = new MySqlCommand("""
            insert into invoices (project_id, client_id, invoice_number, invoice_date, due_date, status, subtotal, tax_amount, total_due, notes, updated_at)
            values (@project_id, @client_id, @invoice_number, @invoice_date, @due_date, 'Issued', 0, 0, 0, '', utc_timestamp(6));
            """, connection, tx);
        insert.Parameters.AddWithValue("@project_id", projectId);
        insert.Parameters.AddWithValue("@client_id", clientId.Value);
        insert.Parameters.AddWithValue("@invoice_number", invoiceNumber);
        insert.Parameters.AddWithValue("@invoice_date", AsDateTime(invoiceDate));
        insert.Parameters.AddWithValue("@due_date", AsDateTime(invoiceDate.AddDays(14)));
        await insert.ExecuteNonQueryAsync();
        var invoiceId = insert.LastInsertedId;

        await using var copy = new MySqlCommand("""
            insert into invoice_items (invoice_id, project_item_id, sort_order, item_category, description, quantity, unit_label, unit_price, cost_amount, line_total)
            select @invoice_id,
                   pi.id,
                   row_number() over(order by pi.incurred_on, pi.id),
                   pi.item_category,
                   pi.description,
                   pi.quantity,
                   pi.unit_label,
                   pi.billable_amount,
                   pi.cost_amount,
                   pi.billable_amount * pi.quantity
            from project_items pi
            where pi.project_id = @project_id;
            """, connection, tx);
        copy.Parameters.AddWithValue("@invoice_id", invoiceId);
        copy.Parameters.AddWithValue("@project_id", projectId);
        await copy.ExecuteNonQueryAsync();

        await using var totals = new MySqlCommand("""
            update invoices
            set subtotal = coalesce((select sum(line_total) from invoice_items where invoice_id = @invoice_id), 0),
                tax_amount = 0,
                total_due = coalesce((select sum(line_total) from invoice_items where invoice_id = @invoice_id), 0),
                updated_at = utc_timestamp(6)
            where id = @invoice_id;
            """, connection, tx);
        totals.Parameters.AddWithValue("@invoice_id", invoiceId);
        await totals.ExecuteNonQueryAsync();

        await UpdateInvoiceStatusAsync(connection, invoiceId, tx);
        await UpdateProjectStatusAsync(connection, projectId, tx);
        await tx.CommitAsync();
        return invoiceId;
    }

    public async Task<IReadOnlyList<InvoiceListItem>> GetInvoicesAsync(DateOnly? from = null, DateOnly? to = null)
    {
        var fromDate = from ?? DateOnly.FromDateTime(DateTime.Today.AddMonths(-3));
        var toDate = to ?? DateOnly.FromDateTime(DateTime.Today);
        await using var connection = OpenConnection();
        await connection.OpenAsync();
        await using var cmd = new MySqlCommand("""
            select i.id, i.invoice_number, i.invoice_date, c.full_name, p.name, i.total_due,
                   coalesce(pay.amount_paid, 0),
                   i.total_due - coalesce(pay.amount_paid, 0),
                   case
                       when i.total_due - coalesce(pay.amount_paid, 0) <= 0 then 'Paid'
                       when coalesce(pay.amount_paid, 0) > 0 then 'Partially Paid'
                       when i.due_date is not null and i.due_date < curdate() then 'Overdue'
                       else i.status
                   end
            from invoices i
            join clients c on c.id = i.client_id
            join projects p on p.id = i.project_id
            left join (
                select invoice_id, sum(amount) as amount_paid
                from invoice_payments
                group by invoice_id
            ) pay on pay.invoice_id = i.id
            where i.invoice_date between @from_date and @to_date
            order by i.invoice_date desc, i.id desc;
            """, connection);
        cmd.Parameters.AddWithValue("@from_date", AsDateTime(fromDate));
        cmd.Parameters.AddWithValue("@to_date", AsDateTime(toDate));
        await using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<InvoiceListItem>();
        while (await reader.ReadAsync())
        {
            list.Add(new InvoiceListItem
            {
                Id = reader.GetInt64(0),
                InvoiceNumber = reader.GetString(1),
                InvoiceDate = DateOnly.FromDateTime(reader.GetDateTime(2)),
                ClientName = reader.GetString(3),
                ProjectName = reader.GetString(4),
                TotalDue = reader.GetDecimal(5),
                AmountPaid = reader.GetDecimal(6),
                Outstanding = reader.GetDecimal(7),
                Status = reader.GetString(8)
            });
        }
        return list;
    }

    public async Task<InvoiceDetail?> GetInvoiceAsync(long invoiceId)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync();
        await using var cmd = new MySqlCommand("""
            select i.id, i.invoice_number, i.invoice_date, i.due_date, i.status, i.subtotal, i.tax_amount, i.total_due,
                   c.full_name, c.email_address, c.phone_number,
                   concat_ws('\n', nullif(c.address_line1, ''), nullif(c.address_line2, ''), nullif(c.suburb, ''), nullif(c.city, ''), nullif(c.postal_code, '')),
                   p.name,
                   coalesce((select sum(amount) from invoice_payments where invoice_id = i.id), 0)
            from invoices i
            join clients c on c.id = i.client_id
            join projects p on p.id = i.project_id
            where i.id = @id;
            """, connection);
        cmd.Parameters.AddWithValue("@id", invoiceId);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        var detail = new InvoiceDetail
        {
            Id = reader.GetInt64(0),
            InvoiceNumber = reader.GetString(1),
            InvoiceDate = DateOnly.FromDateTime(reader.GetDateTime(2)),
            DueDate = reader.IsDBNull(3) ? null : DateOnly.FromDateTime(reader.GetDateTime(3)),
            Status = reader.GetString(4),
            Subtotal = reader.GetDecimal(5),
            TaxAmount = reader.GetDecimal(6),
            TotalDue = reader.GetDecimal(7),
            ClientName = reader.GetString(8),
            ClientEmail = reader.GetString(9),
            ClientPhone = reader.GetString(10),
            ClientAddressBlock = reader.IsDBNull(11) ? string.Empty : reader.GetString(11),
            ProjectName = reader.GetString(12),
            AmountPaid = reader.GetDecimal(13),
            Outstanding = reader.GetDecimal(7) - reader.GetDecimal(13)
        };
        await reader.CloseAsync();

        var items = await GetInvoiceItemsAsync(connection, invoiceId);
        var payments = await GetInvoicePaymentsAsync(connection, invoiceId);

        return new InvoiceDetail
        {
            Id = detail.Id,
            InvoiceNumber = detail.InvoiceNumber,
            InvoiceDate = detail.InvoiceDate,
            DueDate = detail.DueDate,
            Status = detail.Status,
            ClientName = detail.ClientName,
            ClientEmail = detail.ClientEmail,
            ClientPhone = detail.ClientPhone,
            ClientAddressBlock = detail.ClientAddressBlock,
            ProjectName = detail.ProjectName,
            Subtotal = detail.Subtotal,
            TaxAmount = detail.TaxAmount,
            TotalDue = detail.TotalDue,
            AmountPaid = detail.AmountPaid,
            Outstanding = detail.Outstanding,
            Items = items,
            Payments = payments
        };
    }

    public async Task AddPaymentAsync(long invoiceId, InvoicePaymentRecord payment)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync();
        await using var tx = await connection.BeginTransactionAsync();
        await using var cmd = new MySqlCommand("""
            insert into invoice_payments (invoice_id, payment_date, amount, reference, notes)
            values (@invoice_id, @payment_date, @amount, @reference, @notes);
            """, connection, tx);
        cmd.Parameters.AddWithValue("@invoice_id", invoiceId);
        cmd.Parameters.AddWithValue("@payment_date", AsDateTime(payment.PaymentDate));
        cmd.Parameters.AddWithValue("@amount", payment.Amount);
        cmd.Parameters.AddWithValue("@reference", payment.Reference ?? string.Empty);
        cmd.Parameters.AddWithValue("@notes", payment.Notes ?? string.Empty);
        await cmd.ExecuteNonQueryAsync();
        await UpdateInvoiceStatusAsync(connection, invoiceId, tx);
        await UpdateProjectStatusFromInvoiceAsync(connection, invoiceId, tx);
        await tx.CommitAsync();
    }

    private async Task<IReadOnlyList<InvoiceLineItem>> GetInvoiceItemsAsync(MySqlConnection connection, long invoiceId)
    {
        await using var cmd = new MySqlCommand("""
            select id, item_category, description, quantity, unit_label, unit_price, cost_amount, line_total
            from invoice_items
            where invoice_id = @invoice_id
            order by sort_order, id;
            """, connection);
        cmd.Parameters.AddWithValue("@invoice_id", invoiceId);
        await using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<InvoiceLineItem>();
        while (await reader.ReadAsync())
        {
            list.Add(new InvoiceLineItem
            {
                Id = reader.GetInt64(0),
                ItemCategory = reader.GetString(1),
                Description = reader.GetString(2),
                Quantity = reader.GetDecimal(3),
                UnitLabel = reader.GetString(4),
                UnitPrice = reader.GetDecimal(5),
                CostAmount = reader.GetDecimal(6),
                LineTotal = reader.GetDecimal(7)
            });
        }
        return list;
    }

    private async Task<IReadOnlyList<InvoicePaymentRecord>> GetInvoicePaymentsAsync(MySqlConnection connection, long invoiceId)
    {
        await using var cmd = new MySqlCommand("""
            select id, invoice_id, payment_date, amount, reference, notes
            from invoice_payments
            where invoice_id = @invoice_id
            order by payment_date desc, id desc;
            """, connection);
        cmd.Parameters.AddWithValue("@invoice_id", invoiceId);
        await using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<InvoicePaymentRecord>();
        while (await reader.ReadAsync())
        {
            list.Add(new InvoicePaymentRecord
            {
                Id = reader.GetInt64(0),
                InvoiceId = reader.GetInt64(1),
                PaymentDate = DateOnly.FromDateTime(reader.GetDateTime(2)),
                Amount = reader.GetDecimal(3),
                Reference = reader.GetString(4),
                Notes = reader.GetString(5)
            });
        }
        return list;
    }

    private async Task<string> GenerateInvoiceNumberAsync(MySqlConnection connection, MySqlTransaction tx, DateOnly invoiceDate)
    {
        var prefix = invoiceDate.ToString("ddMMyyyy");
        await using var cmd = new MySqlCommand("select count(*) from invoices where invoice_number like concat(@prefix, '-%');", connection, tx);
        cmd.Parameters.AddWithValue("@prefix", prefix);
        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        return $"{prefix}-{count + 1}";
    }

    private static async Task UpdateInvoiceStatusAsync(MySqlConnection connection, long invoiceId, MySqlTransaction? tx = null)
    {
        await using var totalPaidCommand = new MySqlCommand(
            "select coalesce(sum(amount), 0) from invoice_payments where invoice_id = @invoice_id;",
            connection,
            tx);
        totalPaidCommand.Parameters.AddWithValue("@invoice_id", invoiceId);
        var amountPaid = Convert.ToDecimal(await totalPaidCommand.ExecuteScalarAsync());

        await using var invoiceTotalCommand = new MySqlCommand(
            "select total_due from invoices where id = @invoice_id;",
            connection,
            tx);
        invoiceTotalCommand.Parameters.AddWithValue("@invoice_id", invoiceId);
        var totalDue = Convert.ToDecimal(await invoiceTotalCommand.ExecuteScalarAsync());

        var status = amountPaid >= totalDue && totalDue > 0m
            ? "Paid"
            : amountPaid > 0m
                ? "Partially Paid"
                : "Issued";

        await using var update = new MySqlCommand("""
            update invoices
            set status = @status,
                updated_at = utc_timestamp(6)
            where id = @invoice_id;
            """, connection, tx);
        update.Parameters.AddWithValue("@status", status);
        update.Parameters.AddWithValue("@invoice_id", invoiceId);
        await update.ExecuteNonQueryAsync();
    }

    private static async Task UpdateProjectStatusFromInvoiceAsync(MySqlConnection connection, long invoiceId, MySqlTransaction? tx = null)
    {
        await using var cmd = new MySqlCommand("select project_id from invoices where id = @invoice_id;", connection, tx);
        cmd.Parameters.AddWithValue("@invoice_id", invoiceId);
        var projectId = (long?)await cmd.ExecuteScalarAsync();
        if (projectId.HasValue)
        {
            await UpdateProjectStatusAsync(connection, projectId.Value, tx);
        }
    }

    private static async Task UpdateProjectStatusAsync(MySqlConnection connection, long projectId, MySqlTransaction? tx = null)
    {
        await using var projectStatusCommand = new MySqlCommand("select status from projects where id = @project_id;", connection, tx);
        projectStatusCommand.Parameters.AddWithValue("@project_id", projectId);
        var currentStatus = (string?)await projectStatusCommand.ExecuteScalarAsync() ?? "Active";

        if (string.Equals(currentStatus, "Cancelled", StringComparison.Ordinal))
        {
            await using var cancelledUpdate = new MySqlCommand(
                "update projects set updated_at = utc_timestamp(6) where id = @project_id;",
                connection,
                tx);
            cancelledUpdate.Parameters.AddWithValue("@project_id", projectId);
            await cancelledUpdate.ExecuteNonQueryAsync();
            return;
        }

        await using var summaryCommand = new MySqlCommand("""
            select count(*),
                   coalesce(sum(i.total_due), 0),
                   coalesce(sum(coalesce(pay.amount_paid, 0)), 0)
            from invoices i
            left join (
                select invoice_id, sum(amount) as amount_paid
                from invoice_payments
                group by invoice_id
            ) pay on pay.invoice_id = i.id
            where i.project_id = @project_id;
            """, connection, tx);
        summaryCommand.Parameters.AddWithValue("@project_id", projectId);
        await using var reader = await summaryCommand.ExecuteReaderAsync();
        await reader.ReadAsync();
        var invoiceCount = reader.GetInt32(0);
        var totalDue = reader.GetDecimal(1);
        var totalPaid = reader.GetDecimal(2);
        await reader.CloseAsync();

        var nextStatus = invoiceCount switch
        {
            0 when currentStatus is "Closed" => "Closed",
            0 => "Active",
            _ when totalPaid >= totalDue => "Paid",
            _ => "Invoiced"
        };

        await using var update = new MySqlCommand("""
            update projects
            set status = @status,
                updated_at = utc_timestamp(6)
            where id = @project_id;
            """, connection, tx);
        update.Parameters.AddWithValue("@status", nextStatus);
        update.Parameters.AddWithValue("@project_id", projectId);
        await update.ExecuteNonQueryAsync();
    }

    private static void AddClientParams(MySqlCommand cmd, ClientRecord client)
    {
        cmd.Parameters.AddWithValue("@full_name", client.FullName);
        cmd.Parameters.AddWithValue("@email_address", client.EmailAddress ?? string.Empty);
        cmd.Parameters.AddWithValue("@phone_number", client.PhoneNumber ?? string.Empty);
        cmd.Parameters.AddWithValue("@address_line1", client.AddressLine1 ?? string.Empty);
        cmd.Parameters.AddWithValue("@address_line2", client.AddressLine2 ?? string.Empty);
        cmd.Parameters.AddWithValue("@suburb", client.Suburb ?? string.Empty);
        cmd.Parameters.AddWithValue("@city", client.City ?? string.Empty);
        cmd.Parameters.AddWithValue("@postal_code", client.PostalCode ?? string.Empty);
        cmd.Parameters.AddWithValue("@notes", client.Notes ?? string.Empty);
    }

    private static void AddProjectParams(MySqlCommand cmd, ProjectRecord project)
    {
        cmd.Parameters.AddWithValue("@client_id", project.ClientId);
        cmd.Parameters.AddWithValue("@name", project.Name);
        cmd.Parameters.AddWithValue("@description", project.Description ?? string.Empty);
        cmd.Parameters.AddWithValue("@status", project.Status);
        cmd.Parameters.AddWithValue("@start_date", project.StartDate.HasValue ? AsDateTime(project.StartDate.Value) : DBNull.Value);
        cmd.Parameters.AddWithValue("@due_date", project.DueDate.HasValue ? AsDateTime(project.DueDate.Value) : DBNull.Value);
    }

    private static void AddItemParams(MySqlCommand cmd, ProjectItemRecord item)
    {
        cmd.Parameters.AddWithValue("@project_id", item.ProjectId);
        cmd.Parameters.AddWithValue("@item_category", item.ItemCategory);
        cmd.Parameters.AddWithValue("@description", item.Description);
        cmd.Parameters.AddWithValue("@quantity", item.Quantity);
        cmd.Parameters.AddWithValue("@unit_label", item.UnitLabel);
        cmd.Parameters.AddWithValue("@cost_amount", item.CostAmount);
        cmd.Parameters.AddWithValue("@billable_amount", item.BillableAmount);
        cmd.Parameters.AddWithValue("@incurred_on", AsDateTime(item.IncurredOn));
        cmd.Parameters.AddWithValue("@notes", item.Notes ?? string.Empty);
    }

    private MySqlConnection OpenConnection()
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("The MySQL connection string is not configured.");
        }

        return new MySqlConnection(connectionString);
    }

    private static DateTime AsDateTime(DateOnly value) => value.ToDateTime(TimeOnly.MinValue);
}
