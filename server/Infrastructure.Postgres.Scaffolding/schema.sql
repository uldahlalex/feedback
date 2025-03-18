drop schema if exists questions cascade;
create schema if not exists questions;


create table questions.question (
                                    questiontext text not null,
                                    timestamp timestamp with time zone,
                                    id text not null,
                                    CONSTRAINT question_pk PRIMARY KEY(id)
)