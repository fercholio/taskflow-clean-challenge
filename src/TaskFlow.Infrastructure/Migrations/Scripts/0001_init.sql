create extension if not exists "uuid-ossp";

create table if not exists users (
    id              uuid primary key,
    email           varchar(254) unique not null,
    password_hash   text not null,
    created_at_utc  timestamptz not null
);

create table if not exists tasks (
    id              uuid primary key,
    user_id         uuid not null references users(id) on delete cascade,
    title           varchar(200) not null,
    description     varchar(2000) null,
    status          smallint not null,
    due_date_utc    timestamptz not null,
    created_at_utc  timestamptz not null,
    updated_at_utc  timestamptz not null
);

create index if not exists ix_tasks_user_id on tasks(user_id);
