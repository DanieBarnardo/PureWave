create table if not exists intake_submissions
(
    id bigserial primary key,
    submitted_at timestamptz not null default now(),
    full_name varchar(100) not null,
    email_address varchar(256) not null,
    phone_number varchar(64) not null default '',
    suburb_or_area varchar(120) not null,
    project_stage varchar(120) not null,
    interested_plan varchar(120) not null,
    room_type varchar(120) not null,
    primary_goals text not null,
    room_dimensions varchar(400) not null,
    budget_band varchar(120) not null,
    timeline varchar(120) not null,
    needs_acoustic_design boolean not null default false,
    needs_calibration boolean not null default false,
    needs_automation boolean not null default false,
    needs_procurement_advice boolean not null default false,
    needs_existing_equipment_installation boolean not null default false,
    needs_guidance_only boolean not null default false,
    existing_equipment text not null default '',
    key_challenges text not null default '',
    contact_preference varchar(120) not null
);

create index if not exists ix_intake_submissions_submitted_at
    on intake_submissions (submitted_at desc);

create index if not exists ix_intake_submissions_email_address
    on intake_submissions (email_address);
