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
);

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
);

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
);

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
);

create table if not exists invoices
(
    id bigint not null auto_increment primary key,
    project_id bigint not null,
    client_id bigint not null,
    invoice_number varchar(40) not null unique,
    invoice_date date not null,
    due_date date null,
    status varchar(40) not null default 'Issued',
    subtotal decimal(12,2) not null default 0,
    tax_amount decimal(12,2) not null default 0,
    total_due decimal(12,2) not null default 0,
    notes text not null,
    created_at datetime(6) not null default current_timestamp(6),
    updated_at datetime(6) not null default current_timestamp(6),
    index ix_invoices_client_id (client_id),
    index ix_invoices_project_id (project_id),
    constraint fk_invoices_project_id foreign key (project_id) references projects(id) on delete restrict,
    constraint fk_invoices_client_id foreign key (client_id) references clients(id) on delete restrict
);

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
);

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
);

update projects
set status = case
    when status in ('Planned', 'On Hold') then 'Active'
    when status = 'Completed' then 'Closed'
    when status = 'Cancelled' then 'Cancelled'
    else status
end
where status in ('Planned', 'On Hold', 'Completed', 'Cancelled');
