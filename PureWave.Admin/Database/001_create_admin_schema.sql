alter table if exists intake_submissions
    add column if not exists client_id bigint null;

create table if not exists clients
(
    id bigserial primary key,
    full_name varchar(120) not null,
    email_address varchar(256) not null default '',
    phone_number varchar(64) not null default '',
    address_line1 varchar(160) not null default '',
    address_line2 varchar(160) not null default '',
    suburb varchar(80) not null default '',
    city varchar(80) not null default '',
    postal_code varchar(20) not null default '',
    notes text not null default '',
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

do $$
begin
    if not exists (
        select 1
        from information_schema.table_constraints
        where constraint_name = 'fk_intake_submissions_client_id'
          and table_name = 'intake_submissions'
    ) then
        alter table intake_submissions
            add constraint fk_intake_submissions_client_id
            foreign key (client_id) references clients(id) on delete set null;
    end if;
end $$;

create table if not exists projects
(
    id bigserial primary key,
    client_id bigint not null references clients(id) on delete restrict,
    name varchar(140) not null,
    description text not null default '',
    status varchar(40) not null default 'Active',
    start_date date null,
    due_date date null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table if not exists project_items
(
    id bigserial primary key,
    project_id bigint not null references projects(id) on delete cascade,
    item_category varchar(32) not null,
    description varchar(400) not null,
    quantity numeric(12,2) not null,
    unit_label varchar(32) not null default 'Unit',
    cost_amount numeric(12,2) not null default 0,
    billable_amount numeric(12,2) not null default 0,
    incurred_on date not null default current_date,
    notes text not null default '',
    created_at timestamptz not null default now()
);

create table if not exists invoices
(
    id bigserial primary key,
    project_id bigint not null references projects(id) on delete restrict,
    client_id bigint not null references clients(id) on delete restrict,
    invoice_number varchar(40) not null unique,
    invoice_date date not null,
    due_date date null,
    status varchar(40) not null default 'Issued',
    subtotal numeric(12,2) not null default 0,
    tax_amount numeric(12,2) not null default 0,
    total_due numeric(12,2) not null default 0,
    notes text not null default '',
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table if not exists invoice_items
(
    id bigserial primary key,
    invoice_id bigint not null references invoices(id) on delete cascade,
    project_item_id bigint null references project_items(id) on delete set null,
    sort_order integer not null default 1,
    item_category varchar(32) not null,
    description varchar(400) not null,
    quantity numeric(12,2) not null,
    unit_label varchar(32) not null default 'Unit',
    unit_price numeric(12,2) not null default 0,
    cost_amount numeric(12,2) not null default 0,
    line_total numeric(12,2) not null default 0
);

create table if not exists invoice_payments
(
    id bigserial primary key,
    invoice_id bigint not null references invoices(id) on delete cascade,
    payment_date date not null,
    amount numeric(12,2) not null,
    reference varchar(120) not null default '',
    notes text not null default '',
    created_at timestamptz not null default now()
);

create index if not exists ix_projects_client_id on projects(client_id);
create index if not exists ix_project_items_project_id on project_items(project_id);
create index if not exists ix_invoices_client_id on invoices(client_id);
create index if not exists ix_invoices_project_id on invoices(project_id);
create index if not exists ix_invoice_payments_invoice_id on invoice_payments(invoice_id);

update projects
set status = case
    when status in ('Planned', 'On Hold') then 'Active'
    when status = 'Completed' then 'Closed'
    when status = 'Cancelled' then 'Cancelled'
    else status
end
where status in ('Planned', 'On Hold', 'Completed', 'Cancelled');
