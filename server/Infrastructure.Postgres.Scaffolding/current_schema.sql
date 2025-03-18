-- This schema is generated based on the current DBContext. Please check the class Seeder to see.
DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'questions') THEN
        CREATE SCHEMA questions;
    END IF;
END $EF$;


CREATE TABLE questions.question (
    id text NOT NULL,
    questiontext text NOT NULL,
    timestamp timestamp with time zone,
    CONSTRAINT question_pk PRIMARY KEY (id)
);


