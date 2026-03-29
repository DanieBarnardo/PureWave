using Npgsql;

var connectionString = "Server=purewavedb.postgres.database.azure.com;Database=postgres;Port=5432;User Id=danieadmpurewave;Password=Q!w2e3r4t5;Ssl Mode=Require;";

var clientName = "Revz";
var addressLine1 = "83 Methven Road";
var suburb = "Chiltern Hills";
var city = "Westville";
var projectName = "Projector and projector screen installation";
var projectDescription = "Imported from historical invoice 28032026-1 for projector and screen installation.";
var invoiceNumber = "28032026-1";
var invoiceDate = new DateOnly(2026, 3, 28);
var paymentDate = new DateOnly(2026, 3, 29);

var items = new[]
{
    new ImportedItem("Consultancy", "Consultancy (1 Hour)", 1.0m, "Hr", 0.0m, 400.0m, invoiceDate, "Imported from invoice 28032026-1."),
    new ImportedItem("Labor", "Labor (Projector and screen installation)", 8.5m, "Hr", 0.0m, 300.0m, invoiceDate, "Imported from invoice 28032026-1."),
    new ImportedItem("Parts", "Projector Mount (Component)", 1.0m, "Unit", 0.0m, 299.0m, invoiceDate, "Imported from invoice 28032026-1.")
};

try
{
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    await using var transaction = await connection.BeginTransactionAsync();

    var clientId = await EnsureClientAsync(connection, transaction);
    var projectId = await EnsureProjectAsync(connection, transaction, clientId);
    await EnsureProjectItemsAsync(connection, transaction, projectId);
    var invoiceId = await EnsureInvoiceAsync(connection, transaction, clientId, projectId);
    var paymentId = await EnsurePaymentAsync(connection, transaction, invoiceId);

    await UpdateStatusesAsync(connection, transaction, projectId, invoiceId);

    await transaction.CommitAsync();

    Console.WriteLine($"ClientId={clientId}");
    Console.WriteLine($"ProjectId={projectId}");
    Console.WriteLine($"InvoiceId={invoiceId}");
    Console.WriteLine($"PaymentId={paymentId}");
}
catch (Exception ex)
{
    Console.WriteLine("Historical import failed.");
    Console.WriteLine(ex.GetType().FullName);
    Console.WriteLine(ex.Message);
    Environment.ExitCode = 1;
}

async Task<long> EnsureClientAsync(NpgsqlConnection connection, NpgsqlTransaction transaction)
{
    await using var existingCommand = new NpgsqlCommand(
        """
        select id
        from clients
        where full_name = @full_name and address_line1 = @address_line1
        limit 1;
        """,
        connection,
        transaction);
    existingCommand.Parameters.AddWithValue("full_name", clientName);
    existingCommand.Parameters.AddWithValue("address_line1", addressLine1);
    var existingId = await existingCommand.ExecuteScalarAsync();
    if (existingId is long foundId)
    {
        return foundId;
    }

    await using var insertCommand = new NpgsqlCommand(
        """
        insert into clients (full_name, email_address, phone_number, address_line1, address_line2, suburb, city, postal_code, notes, updated_at)
        values (@full_name, '', '', @address_line1, '', @suburb, @city, '', @notes, now())
        returning id;
        """,
        connection,
        transaction);
    insertCommand.Parameters.AddWithValue("full_name", clientName);
    insertCommand.Parameters.AddWithValue("address_line1", addressLine1);
    insertCommand.Parameters.AddWithValue("suburb", suburb);
    insertCommand.Parameters.AddWithValue("city", city);
    insertCommand.Parameters.AddWithValue("notes", "Imported from historical invoice and proof of payment PDFs on 2026-03-29.");
    return (long)(await insertCommand.ExecuteScalarAsync() ?? 0L);
}

async Task<long> EnsureProjectAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, long clientId)
{
    await using var existingCommand = new NpgsqlCommand(
        """
        select id
        from projects
        where client_id = @client_id and name = @name
        limit 1;
        """,
        connection,
        transaction);
    existingCommand.Parameters.AddWithValue("client_id", clientId);
    existingCommand.Parameters.AddWithValue("name", projectName);
    var existingId = await existingCommand.ExecuteScalarAsync();
    if (existingId is long foundId)
    {
        return foundId;
    }

    await using var insertCommand = new NpgsqlCommand(
        """
        insert into projects (client_id, name, description, status, start_date, due_date, updated_at)
        values (@client_id, @name, @description, 'Paid', @start_date, @due_date, now())
        returning id;
        """,
        connection,
        transaction);
    insertCommand.Parameters.AddWithValue("client_id", clientId);
    insertCommand.Parameters.AddWithValue("name", projectName);
    insertCommand.Parameters.AddWithValue("description", projectDescription);
    insertCommand.Parameters.AddWithValue("start_date", invoiceDate);
    insertCommand.Parameters.AddWithValue("due_date", paymentDate);
    return (long)(await insertCommand.ExecuteScalarAsync() ?? 0L);
}

async Task EnsureProjectItemsAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, long projectId)
{
    foreach (var item in items)
    {
        await using var existingCommand = new NpgsqlCommand(
            """
            select id
            from project_items
            where project_id = @project_id and description = @description
            limit 1;
            """,
            connection,
            transaction);
        existingCommand.Parameters.AddWithValue("project_id", projectId);
        existingCommand.Parameters.AddWithValue("description", item.Description);
        var existingId = await existingCommand.ExecuteScalarAsync();
        if (existingId is not null)
        {
            continue;
        }

        await using var insertCommand = new NpgsqlCommand(
            """
            insert into project_items (project_id, item_category, description, quantity, unit_label, cost_amount, billable_amount, incurred_on, notes)
            values (@project_id, @item_category, @description, @quantity, @unit_label, @cost_amount, @billable_amount, @incurred_on, @notes);
            """,
            connection,
            transaction);
        insertCommand.Parameters.AddWithValue("project_id", projectId);
        insertCommand.Parameters.AddWithValue("item_category", item.Category);
        insertCommand.Parameters.AddWithValue("description", item.Description);
        insertCommand.Parameters.AddWithValue("quantity", item.Quantity);
        insertCommand.Parameters.AddWithValue("unit_label", item.UnitLabel);
        insertCommand.Parameters.AddWithValue("cost_amount", item.CostAmount);
        insertCommand.Parameters.AddWithValue("billable_amount", item.BillableAmount);
        insertCommand.Parameters.AddWithValue("incurred_on", item.IncurredOn);
        insertCommand.Parameters.AddWithValue("notes", item.Notes);
        await insertCommand.ExecuteNonQueryAsync();
    }
}

async Task<long> EnsureInvoiceAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, long clientId, long projectId)
{
    await using var existingCommand = new NpgsqlCommand(
        """
        select id
        from invoices
        where invoice_number = @invoice_number
        limit 1;
        """,
        connection,
        transaction);
    existingCommand.Parameters.AddWithValue("invoice_number", invoiceNumber);
    var existingId = await existingCommand.ExecuteScalarAsync();
    if (existingId is long foundId)
    {
        return foundId;
    }

    await using var insertCommand = new NpgsqlCommand(
        """
        insert into invoices (project_id, client_id, invoice_number, invoice_date, due_date, status, subtotal, tax_amount, total_due, notes, updated_at)
        values (@project_id, @client_id, @invoice_number, @invoice_date, @due_date, 'Paid', 3249.00, 0.00, 3249.00, @notes, now())
        returning id;
        """,
        connection,
        transaction);
    insertCommand.Parameters.AddWithValue("project_id", projectId);
    insertCommand.Parameters.AddWithValue("client_id", clientId);
    insertCommand.Parameters.AddWithValue("invoice_number", invoiceNumber);
    insertCommand.Parameters.AddWithValue("invoice_date", invoiceDate);
    insertCommand.Parameters.AddWithValue("due_date", paymentDate);
    insertCommand.Parameters.AddWithValue("notes", "Imported from historical invoice PDF on 2026-03-29.");
    var invoiceId = (long)(await insertCommand.ExecuteScalarAsync() ?? 0L);

    for (var i = 0; i < items.Length; i++)
    {
        var item = items[i];
        await using var insertItemCommand = new NpgsqlCommand(
            """
            insert into invoice_items (invoice_id, project_item_id, sort_order, item_category, description, quantity, unit_label, unit_price, cost_amount, line_total)
            select @invoice_id, pi.id, @sort_order, @item_category, @description, @quantity, @unit_label, @unit_price, @cost_amount, @line_total
            from project_items pi
            where pi.project_id = @project_id and pi.description = @description
            limit 1;
            """,
            connection,
            transaction);
        insertItemCommand.Parameters.AddWithValue("invoice_id", invoiceId);
        insertItemCommand.Parameters.AddWithValue("project_id", projectId);
        insertItemCommand.Parameters.AddWithValue("sort_order", i + 1);
        insertItemCommand.Parameters.AddWithValue("item_category", item.Category);
        insertItemCommand.Parameters.AddWithValue("description", item.Description);
        insertItemCommand.Parameters.AddWithValue("quantity", item.Quantity);
        insertItemCommand.Parameters.AddWithValue("unit_label", item.UnitLabel);
        insertItemCommand.Parameters.AddWithValue("unit_price", item.BillableAmount);
        insertItemCommand.Parameters.AddWithValue("cost_amount", item.CostAmount);
        insertItemCommand.Parameters.AddWithValue("line_total", item.Quantity * item.BillableAmount);
        await insertItemCommand.ExecuteNonQueryAsync();
    }

    return invoiceId;
}

async Task<long> EnsurePaymentAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, long invoiceId)
{
    await using var existingCommand = new NpgsqlCommand(
        """
        select id
        from invoice_payments
        where invoice_id = @invoice_id and payment_date = @payment_date and amount = 3249.00
        limit 1;
        """,
        connection,
        transaction);
    existingCommand.Parameters.AddWithValue("invoice_id", invoiceId);
    existingCommand.Parameters.AddWithValue("payment_date", paymentDate);
    var existingId = await existingCommand.ExecuteScalarAsync();
    if (existingId is long foundId)
    {
        return foundId;
    }

    await using var insertCommand = new NpgsqlCommand(
        """
        insert into invoice_payments (invoice_id, payment_date, amount, reference, notes)
        values (@invoice_id, @payment_date, 3249.00, @reference, @notes)
        returning id;
        """,
        connection,
        transaction);
    insertCommand.Parameters.AddWithValue("invoice_id", invoiceId);
    insertCommand.Parameters.AddWithValue("payment_date", paymentDate);
    insertCommand.Parameters.AddWithValue("reference", "NoticeOfPayment");
    insertCommand.Parameters.AddWithValue("notes", "Imported from NoticeOfPayment.pdf on 2026-03-29.");
    return (long)(await insertCommand.ExecuteScalarAsync() ?? 0L);
}

async Task UpdateStatusesAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, long projectId, long invoiceId)
{
    await using var invoiceCommand = new NpgsqlCommand(
        "update invoices set status = 'Paid', updated_at = now() where id = @invoice_id;",
        connection,
        transaction);
    invoiceCommand.Parameters.AddWithValue("invoice_id", invoiceId);
    await invoiceCommand.ExecuteNonQueryAsync();

    await using var projectCommand = new NpgsqlCommand(
        "update projects set status = 'Paid', updated_at = now() where id = @project_id;",
        connection,
        transaction);
    projectCommand.Parameters.AddWithValue("project_id", projectId);
    await projectCommand.ExecuteNonQueryAsync();
}

internal sealed record ImportedItem(
    string Category,
    string Description,
    decimal Quantity,
    string UnitLabel,
    decimal CostAmount,
    decimal BillableAmount,
    DateOnly IncurredOn,
    string Notes);
