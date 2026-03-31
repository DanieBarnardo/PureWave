using System.Globalization;
using MySqlConnector;

const string SourceConnectionString = "Server=purewavedb.mysql.database.azure.com;Port=3306;UserID=danieadmpurewave;Password=Q!w2e3r4t5;Database=purewave;SslMode=Required;";
const string TargetConnectionString = "Server=mysqlssd2.zadns.co.za;Port=3307;UserID=danieadmpurewave;Password=Q!w2e3r4t5;Database=purewavedb;SslMode=Required;";

var tableNames = new[]
{
    "clients",
    "intake_submissions",
    "projects",
    "project_items",
    "invoices",
    "invoice_items",
    "invoice_payments"
};

try
{
    await using var source = new MySqlConnection(SourceConnectionString);
    await using var target = new MySqlConnection(TargetConnectionString);

    await source.OpenAsync();
    await target.OpenAsync();

    await using var transaction = await target.BeginTransactionAsync();

    await ExecuteNonQueryAsync(target, transaction, "set foreign_key_checks = 0;");
    await EnsureSchemaAsync(target, transaction);
    await TruncateTablesAsync(target, transaction, tableNames);

    await CopyClientsAsync(source, target, transaction);
    await CopyIntakeSubmissionsAsync(source, target, transaction);
    await CopyProjectsAsync(source, target, transaction);
    await CopyProjectItemsAsync(source, target, transaction);
    await CopyInvoicesAsync(source, target, transaction);
    await CopyInvoiceItemsAsync(source, target, transaction);
    await CopyInvoicePaymentsAsync(source, target, transaction);

    await ResetAutoIncrementAsync(target, transaction, tableNames);
    await ExecuteNonQueryAsync(target, transaction, "set foreign_key_checks = 1;");

    await transaction.CommitAsync();

    Console.WriteLine("Migration completed.");
    foreach (var (tableName, count) in await GetCountsAsync(target, tableNames))
    {
        Console.WriteLine($"{tableName}={count}");
    }
}
catch (Exception ex)
{
    Console.WriteLine("MySQL-to-MySQL migration failed.");
    Console.WriteLine(ex.GetType().FullName);
    Console.WriteLine(ex.Message);
    Environment.ExitCode = 1;
}

static async Task EnsureSchemaAsync(MySqlConnection connection, MySqlTransaction transaction)
{
    var statements = new[]
    {
        """
        create table if not exists clients
        (
            id bigint not null auto_increment primary key,
            full_name varchar(120) not null,
            email_address varchar(256) not null default '',
            phone_number varchar(64) not null default '',
            address_line1 varchar(160) not null default '',
            address_line2 varchar(160) not null default '',
            suburb varchar(80) not null default '',
            city varchar(80) not null default '',
            postal_code varchar(20) not null default '',
            notes text not null,
            created_at datetime(6) not null default current_timestamp(6),
            updated_at datetime(6) not null default current_timestamp(6)
        ) engine=InnoDB default charset=utf8mb4 collate=utf8mb4_unicode_ci;
        """,
        """
        create table if not exists intake_submissions
        (
            id bigint not null auto_increment primary key,
            submitted_at datetime(6) not null default current_timestamp(6),
            full_name varchar(100) not null,
            email_address varchar(256) not null,
            phone_number varchar(64) not null default '',
            suburb_or_area varchar(120) not null,
            project_stage varchar(120) not null,
            interested_plan varchar(120) not null,
            service_mode varchar(120) not null default '',
            service_format varchar(120) not null default '',
            room_type varchar(120) not null,
            primary_goals text not null,
            room_dimensions varchar(400) not null,
            budget_band varchar(120) not null,
            timeline varchar(120) not null,
            needs_acoustic_design tinyint(1) not null default 0,
            needs_calibration tinyint(1) not null default 0,
            needs_automation tinyint(1) not null default 0,
            needs_procurement_advice tinyint(1) not null default 0,
            needs_existing_equipment_installation tinyint(1) not null default 0,
            needs_guidance_only tinyint(1) not null default 0,
            existing_equipment text not null,
            key_challenges text not null,
            contact_preference varchar(120) not null,
            client_id bigint null,
            index ix_intake_submissions_submitted_at (submitted_at),
            index ix_intake_submissions_email_address (email_address),
            constraint fk_intake_submissions_client_id foreign key (client_id) references clients(id) on delete set null
        ) engine=InnoDB default charset=utf8mb4 collate=utf8mb4_unicode_ci;
        """,
        """
        create table if not exists projects
        (
            id bigint not null auto_increment primary key,
            client_id bigint not null,
            name varchar(140) not null,
            description text not null,
            status varchar(40) not null default 'Active',
            start_date date null,
            due_date date null,
            created_at datetime(6) not null default current_timestamp(6),
            updated_at datetime(6) not null default current_timestamp(6),
            index ix_projects_client_id (client_id),
            constraint fk_projects_client_id foreign key (client_id) references clients(id) on delete restrict
        ) engine=InnoDB default charset=utf8mb4 collate=utf8mb4_unicode_ci;
        """,
        """
        create table if not exists project_items
        (
            id bigint not null auto_increment primary key,
            project_id bigint not null,
            item_category varchar(32) not null,
            description varchar(400) not null,
            quantity decimal(12,2) not null,
            unit_label varchar(32) not null default 'Unit',
            cost_amount decimal(12,2) not null default 0,
            billable_amount decimal(12,2) not null default 0,
            incurred_on date not null,
            notes text not null,
            created_at datetime(6) not null default current_timestamp(6),
            index ix_project_items_project_id (project_id),
            constraint fk_project_items_project_id foreign key (project_id) references projects(id) on delete cascade
        ) engine=InnoDB default charset=utf8mb4 collate=utf8mb4_unicode_ci;
        """,
        """
        create table if not exists invoices
        (
            id bigint not null auto_increment primary key,
            project_id bigint not null,
            client_id bigint not null,
            invoice_number varchar(40) not null,
            invoice_date date not null,
            due_date date null,
            status varchar(40) not null default 'Issued',
            subtotal decimal(12,2) not null default 0,
            tax_amount decimal(12,2) not null default 0,
            total_due decimal(12,2) not null default 0,
            notes text not null,
            created_at datetime(6) not null default current_timestamp(6),
            updated_at datetime(6) not null default current_timestamp(6),
            unique key ux_invoices_invoice_number (invoice_number),
            index ix_invoices_client_id (client_id),
            index ix_invoices_project_id (project_id),
            constraint fk_invoices_project_id foreign key (project_id) references projects(id) on delete restrict,
            constraint fk_invoices_client_id foreign key (client_id) references clients(id) on delete restrict
        ) engine=InnoDB default charset=utf8mb4 collate=utf8mb4_unicode_ci;
        """,
        """
        create table if not exists invoice_items
        (
            id bigint not null auto_increment primary key,
            invoice_id bigint not null,
            project_item_id bigint null,
            sort_order int not null default 1,
            item_category varchar(32) not null,
            description varchar(400) not null,
            quantity decimal(12,2) not null,
            unit_label varchar(32) not null default 'Unit',
            unit_price decimal(12,2) not null default 0,
            cost_amount decimal(12,2) not null default 0,
            line_total decimal(12,2) not null default 0,
            constraint fk_invoice_items_invoice_id foreign key (invoice_id) references invoices(id) on delete cascade,
            constraint fk_invoice_items_project_item_id foreign key (project_item_id) references project_items(id) on delete set null
        ) engine=InnoDB default charset=utf8mb4 collate=utf8mb4_unicode_ci;
        """,
        """
        create table if not exists invoice_payments
        (
            id bigint not null auto_increment primary key,
            invoice_id bigint not null,
            payment_date date not null,
            amount decimal(12,2) not null,
            reference varchar(120) not null default '',
            notes text not null,
            created_at datetime(6) not null default current_timestamp(6),
            index ix_invoice_payments_invoice_id (invoice_id),
            constraint fk_invoice_payments_invoice_id foreign key (invoice_id) references invoices(id) on delete cascade
        ) engine=InnoDB default charset=utf8mb4 collate=utf8mb4_unicode_ci;
        """
    };

    foreach (var statement in statements)
    {
        await ExecuteNonQueryAsync(connection, transaction, statement);
    }
}

static async Task TruncateTablesAsync(MySqlConnection connection, MySqlTransaction transaction, IEnumerable<string> tableNames)
{
    foreach (var tableName in tableNames.Reverse())
    {
        await ExecuteNonQueryAsync(connection, transaction, $"truncate table `{tableName}`;");
    }
}

static async Task CopyClientsAsync(MySqlConnection source, MySqlConnection target, MySqlTransaction transaction)
{
    const string selectSql = """
        select id, full_name, email_address, phone_number, address_line1, address_line2, suburb, city, postal_code, notes, created_at, updated_at
        from clients
        order by id;
        """;

    const string insertSql = """
        insert into clients
        (id, full_name, email_address, phone_number, address_line1, address_line2, suburb, city, postal_code, notes, created_at, updated_at)
        values
        (@id, @full_name, @email_address, @phone_number, @address_line1, @address_line2, @suburb, @city, @postal_code, @notes, @created_at, @updated_at);
        """;

    await CopyRowsAsync(source, target, transaction, selectSql, insertSql, static (reader, command) =>
    {
        command.Parameters.AddWithValue("@id", reader.GetInt64(0));
        command.Parameters.AddWithValue("@full_name", reader.GetString(1));
        command.Parameters.AddWithValue("@email_address", reader.GetString(2));
        command.Parameters.AddWithValue("@phone_number", reader.GetString(3));
        command.Parameters.AddWithValue("@address_line1", reader.GetString(4));
        command.Parameters.AddWithValue("@address_line2", reader.GetString(5));
        command.Parameters.AddWithValue("@suburb", reader.GetString(6));
        command.Parameters.AddWithValue("@city", reader.GetString(7));
        command.Parameters.AddWithValue("@postal_code", reader.GetString(8));
        command.Parameters.AddWithValue("@notes", reader.GetString(9));
        command.Parameters.AddWithValue("@created_at", reader.GetDateTime(10));
        command.Parameters.AddWithValue("@updated_at", reader.GetDateTime(11));
    });
}

static async Task CopyIntakeSubmissionsAsync(MySqlConnection source, MySqlConnection target, MySqlTransaction transaction)
{
    const string selectSql = """
        select id, submitted_at, full_name, email_address, phone_number, suburb_or_area, project_stage, interested_plan, service_mode, service_format,
               room_type, primary_goals, room_dimensions, budget_band, timeline, needs_acoustic_design, needs_calibration, needs_automation,
               needs_procurement_advice, needs_existing_equipment_installation, needs_guidance_only, existing_equipment, key_challenges,
               contact_preference, client_id
        from intake_submissions
        order by id;
        """;

    const string insertSql = """
        insert into intake_submissions
        (id, submitted_at, full_name, email_address, phone_number, suburb_or_area, project_stage, interested_plan, service_mode, service_format,
         room_type, primary_goals, room_dimensions, budget_band, timeline, needs_acoustic_design, needs_calibration, needs_automation,
         needs_procurement_advice, needs_existing_equipment_installation, needs_guidance_only, existing_equipment, key_challenges,
         contact_preference, client_id)
        values
        (@id, @submitted_at, @full_name, @email_address, @phone_number, @suburb_or_area, @project_stage, @interested_plan, @service_mode, @service_format,
         @room_type, @primary_goals, @room_dimensions, @budget_band, @timeline, @needs_acoustic_design, @needs_calibration, @needs_automation,
         @needs_procurement_advice, @needs_existing_equipment_installation, @needs_guidance_only, @existing_equipment, @key_challenges,
         @contact_preference, @client_id);
        """;

    await CopyRowsAsync(source, target, transaction, selectSql, insertSql, static (reader, command) =>
    {
        command.Parameters.AddWithValue("@id", reader.GetInt64(0));
        command.Parameters.AddWithValue("@submitted_at", reader.GetDateTime(1));
        command.Parameters.AddWithValue("@full_name", reader.GetString(2));
        command.Parameters.AddWithValue("@email_address", reader.GetString(3));
        command.Parameters.AddWithValue("@phone_number", reader.GetString(4));
        command.Parameters.AddWithValue("@suburb_or_area", reader.GetString(5));
        command.Parameters.AddWithValue("@project_stage", reader.GetString(6));
        command.Parameters.AddWithValue("@interested_plan", reader.GetString(7));
        command.Parameters.AddWithValue("@service_mode", reader.GetString(8));
        command.Parameters.AddWithValue("@service_format", reader.GetString(9));
        command.Parameters.AddWithValue("@room_type", reader.GetString(10));
        command.Parameters.AddWithValue("@primary_goals", reader.GetString(11));
        command.Parameters.AddWithValue("@room_dimensions", reader.GetString(12));
        command.Parameters.AddWithValue("@budget_band", reader.GetString(13));
        command.Parameters.AddWithValue("@timeline", reader.GetString(14));
        command.Parameters.AddWithValue("@needs_acoustic_design", reader.GetBoolean(15));
        command.Parameters.AddWithValue("@needs_calibration", reader.GetBoolean(16));
        command.Parameters.AddWithValue("@needs_automation", reader.GetBoolean(17));
        command.Parameters.AddWithValue("@needs_procurement_advice", reader.GetBoolean(18));
        command.Parameters.AddWithValue("@needs_existing_equipment_installation", reader.GetBoolean(19));
        command.Parameters.AddWithValue("@needs_guidance_only", reader.GetBoolean(20));
        command.Parameters.AddWithValue("@existing_equipment", reader.GetString(21));
        command.Parameters.AddWithValue("@key_challenges", reader.GetString(22));
        command.Parameters.AddWithValue("@contact_preference", reader.GetString(23));
        command.Parameters.AddWithValue("@client_id", reader.IsDBNull(24) ? DBNull.Value : reader.GetInt64(24));
    });
}

static async Task CopyProjectsAsync(MySqlConnection source, MySqlConnection target, MySqlTransaction transaction)
{
    const string selectSql = """
        select id, client_id, name, description, status, start_date, due_date, created_at, updated_at
        from projects
        order by id;
        """;

    const string insertSql = """
        insert into projects
        (id, client_id, name, description, status, start_date, due_date, created_at, updated_at)
        values
        (@id, @client_id, @name, @description, @status, @start_date, @due_date, @created_at, @updated_at);
        """;

    await CopyRowsAsync(source, target, transaction, selectSql, insertSql, static (reader, command) =>
    {
        command.Parameters.AddWithValue("@id", reader.GetInt64(0));
        command.Parameters.AddWithValue("@client_id", reader.GetInt64(1));
        command.Parameters.AddWithValue("@name", reader.GetString(2));
        command.Parameters.AddWithValue("@description", reader.GetString(3));
        command.Parameters.AddWithValue("@status", reader.GetString(4));
        command.Parameters.AddWithValue("@start_date", reader.IsDBNull(5) ? DBNull.Value : reader.GetDateTime(5));
        command.Parameters.AddWithValue("@due_date", reader.IsDBNull(6) ? DBNull.Value : reader.GetDateTime(6));
        command.Parameters.AddWithValue("@created_at", reader.GetDateTime(7));
        command.Parameters.AddWithValue("@updated_at", reader.GetDateTime(8));
    });
}

static async Task CopyProjectItemsAsync(MySqlConnection source, MySqlConnection target, MySqlTransaction transaction)
{
    const string selectSql = """
        select id, project_id, item_category, description, quantity, unit_label, cost_amount, billable_amount, incurred_on, notes, created_at
        from project_items
        order by id;
        """;

    const string insertSql = """
        insert into project_items
        (id, project_id, item_category, description, quantity, unit_label, cost_amount, billable_amount, incurred_on, notes, created_at)
        values
        (@id, @project_id, @item_category, @description, @quantity, @unit_label, @cost_amount, @billable_amount, @incurred_on, @notes, @created_at);
        """;

    await CopyRowsAsync(source, target, transaction, selectSql, insertSql, static (reader, command) =>
    {
        command.Parameters.AddWithValue("@id", reader.GetInt64(0));
        command.Parameters.AddWithValue("@project_id", reader.GetInt64(1));
        command.Parameters.AddWithValue("@item_category", reader.GetString(2));
        command.Parameters.AddWithValue("@description", reader.GetString(3));
        command.Parameters.AddWithValue("@quantity", reader.GetDecimal(4));
        command.Parameters.AddWithValue("@unit_label", reader.GetString(5));
        command.Parameters.AddWithValue("@cost_amount", reader.GetDecimal(6));
        command.Parameters.AddWithValue("@billable_amount", reader.GetDecimal(7));
        command.Parameters.AddWithValue("@incurred_on", reader.GetDateTime(8));
        command.Parameters.AddWithValue("@notes", reader.GetString(9));
        command.Parameters.AddWithValue("@created_at", reader.GetDateTime(10));
    });
}

static async Task CopyInvoicesAsync(MySqlConnection source, MySqlConnection target, MySqlTransaction transaction)
{
    const string selectSql = """
        select id, project_id, client_id, invoice_number, invoice_date, due_date, status, subtotal, tax_amount, total_due, notes, created_at, updated_at
        from invoices
        order by id;
        """;

    const string insertSql = """
        insert into invoices
        (id, project_id, client_id, invoice_number, invoice_date, due_date, status, subtotal, tax_amount, total_due, notes, created_at, updated_at)
        values
        (@id, @project_id, @client_id, @invoice_number, @invoice_date, @due_date, @status, @subtotal, @tax_amount, @total_due, @notes, @created_at, @updated_at);
        """;

    await CopyRowsAsync(source, target, transaction, selectSql, insertSql, static (reader, command) =>
    {
        command.Parameters.AddWithValue("@id", reader.GetInt64(0));
        command.Parameters.AddWithValue("@project_id", reader.GetInt64(1));
        command.Parameters.AddWithValue("@client_id", reader.GetInt64(2));
        command.Parameters.AddWithValue("@invoice_number", reader.GetString(3));
        command.Parameters.AddWithValue("@invoice_date", reader.GetDateTime(4));
        command.Parameters.AddWithValue("@due_date", reader.IsDBNull(5) ? DBNull.Value : reader.GetDateTime(5));
        command.Parameters.AddWithValue("@status", reader.GetString(6));
        command.Parameters.AddWithValue("@subtotal", reader.GetDecimal(7));
        command.Parameters.AddWithValue("@tax_amount", reader.GetDecimal(8));
        command.Parameters.AddWithValue("@total_due", reader.GetDecimal(9));
        command.Parameters.AddWithValue("@notes", reader.GetString(10));
        command.Parameters.AddWithValue("@created_at", reader.GetDateTime(11));
        command.Parameters.AddWithValue("@updated_at", reader.GetDateTime(12));
    });
}

static async Task CopyInvoiceItemsAsync(MySqlConnection source, MySqlConnection target, MySqlTransaction transaction)
{
    const string selectSql = """
        select id, invoice_id, project_item_id, sort_order, item_category, description, quantity, unit_label, unit_price, cost_amount, line_total
        from invoice_items
        order by id;
        """;

    const string insertSql = """
        insert into invoice_items
        (id, invoice_id, project_item_id, sort_order, item_category, description, quantity, unit_label, unit_price, cost_amount, line_total)
        values
        (@id, @invoice_id, @project_item_id, @sort_order, @item_category, @description, @quantity, @unit_label, @unit_price, @cost_amount, @line_total);
        """;

    await CopyRowsAsync(source, target, transaction, selectSql, insertSql, static (reader, command) =>
    {
        command.Parameters.AddWithValue("@id", reader.GetInt64(0));
        command.Parameters.AddWithValue("@invoice_id", reader.GetInt64(1));
        command.Parameters.AddWithValue("@project_item_id", reader.IsDBNull(2) ? DBNull.Value : reader.GetInt64(2));
        command.Parameters.AddWithValue("@sort_order", reader.GetInt32(3));
        command.Parameters.AddWithValue("@item_category", reader.GetString(4));
        command.Parameters.AddWithValue("@description", reader.GetString(5));
        command.Parameters.AddWithValue("@quantity", reader.GetDecimal(6));
        command.Parameters.AddWithValue("@unit_label", reader.GetString(7));
        command.Parameters.AddWithValue("@unit_price", reader.GetDecimal(8));
        command.Parameters.AddWithValue("@cost_amount", reader.GetDecimal(9));
        command.Parameters.AddWithValue("@line_total", reader.GetDecimal(10));
    });
}

static async Task CopyInvoicePaymentsAsync(MySqlConnection source, MySqlConnection target, MySqlTransaction transaction)
{
    const string selectSql = """
        select id, invoice_id, payment_date, amount, reference, notes, created_at
        from invoice_payments
        order by id;
        """;

    const string insertSql = """
        insert into invoice_payments
        (id, invoice_id, payment_date, amount, reference, notes, created_at)
        values
        (@id, @invoice_id, @payment_date, @amount, @reference, @notes, @created_at);
        """;

    await CopyRowsAsync(source, target, transaction, selectSql, insertSql, static (reader, command) =>
    {
        command.Parameters.AddWithValue("@id", reader.GetInt64(0));
        command.Parameters.AddWithValue("@invoice_id", reader.GetInt64(1));
        command.Parameters.AddWithValue("@payment_date", reader.GetDateTime(2));
        command.Parameters.AddWithValue("@amount", reader.GetDecimal(3));
        command.Parameters.AddWithValue("@reference", reader.GetString(4));
        command.Parameters.AddWithValue("@notes", reader.GetString(5));
        command.Parameters.AddWithValue("@created_at", reader.GetDateTime(6));
    });
}

static async Task CopyRowsAsync(
    MySqlConnection source,
    MySqlConnection target,
    MySqlTransaction transaction,
    string selectSql,
    string insertSql,
    Action<MySqlDataReader, MySqlCommand> bindParameters)
{
    await using var selectCommand = new MySqlCommand(selectSql, source);
    await using var reader = await selectCommand.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        await using var insertCommand = new MySqlCommand(insertSql, target, transaction);
        bindParameters(reader, insertCommand);
        await insertCommand.ExecuteNonQueryAsync();
    }
}

static async Task ResetAutoIncrementAsync(MySqlConnection connection, MySqlTransaction transaction, IEnumerable<string> tableNames)
{
    foreach (var tableName in tableNames)
    {
        await using var nextIdCommand = new MySqlCommand($"select coalesce(max(id), 0) + 1 from `{tableName}`;", connection, transaction);
        var nextId = Convert.ToInt64(await nextIdCommand.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
        await ExecuteNonQueryAsync(connection, transaction, $"alter table `{tableName}` auto_increment = {nextId};");
    }
}

static async Task<IReadOnlyList<(string TableName, long Count)>> GetCountsAsync(MySqlConnection connection, IEnumerable<string> tableNames)
{
    var counts = new List<(string TableName, long Count)>();

    foreach (var tableName in tableNames)
    {
        await using var command = new MySqlCommand($"select count(*) from `{tableName}`;", connection);
        var count = Convert.ToInt64(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
        counts.Add((tableName, count));
    }

    return counts;
}

static async Task ExecuteNonQueryAsync(MySqlConnection connection, MySqlTransaction transaction, string sql)
{
    await using var command = new MySqlCommand(sql, connection, transaction);
    await command.ExecuteNonQueryAsync();
}
