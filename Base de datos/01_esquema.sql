-- =====================================================================
-- Esquema de usuarios
-- Se ejecuta una sola vez, al crear la base (docker-entrypoint-initdb.d).
-- Los nombres van en snake_case: es la convención de PostgreSQL y evita
-- tener que entrecomillar cada columna ("FechaNacimiento").
-- =====================================================================

-- pgcrypto solo lo usa el script de datos iniciales para generar los
-- hashes BCrypt de los usuarios de ejemplo. La API hashea con su propia
-- librería y no depende de la extensión.
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- El Id sale de una secuencia explícita, como pide el enunciado.
CREATE SEQUENCE usuarios_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO CYCLE;

CREATE TABLE usuarios (
    id                 integer      NOT NULL DEFAULT nextval('usuarios_id_seq'),
    nombres            varchar(100) NOT NULL,
    apellidos          varchar(100) NOT NULL,
    fecha_nacimiento   date         NOT NULL,
    direccion          varchar(250) NOT NULL,
    -- Hash BCrypt: mide 60, el tope de 120 deja margen para cambiar de algoritmo.
    password           varchar(120) NOT NULL,
    telefono           varchar(20)  NOT NULL,
    email              varchar(150) NOT NULL,
    -- El enunciado lo deja nullable, pero con NULL el CHECK no aplica y el
    -- usuario quedaría sin estado. Por eso NOT NULL y por defecto activo.
    estado             varchar(1)   NOT NULL DEFAULT 'A',
    -- Las dos fechas las llenan los triggers de abajo, nunca la aplicación.
    fecha_creacion     timestamptz  NOT NULL,
    fecha_modificacion timestamptz  NULL,

    CONSTRAINT pk_usuarios PRIMARY KEY (id),
    CONSTRAINT ck_usuarios_estado CHECK (estado IN ('A', 'I')),
    CONSTRAINT uq_usuarios_telefono UNIQUE (telefono)
);

ALTER SEQUENCE usuarios_id_seq OWNED BY usuarios.id;

-- El email es único sin distinguir mayúsculas: Ana@x.com y ana@x.com son la misma cuenta.
CREATE UNIQUE INDEX uq_usuarios_email ON usuarios (lower(email));

-- El listado se ordena por apellidos y nombres; el índice evita ordenar en memoria.
CREATE INDEX ix_usuarios_apellidos_nombres ON usuarios (apellidos, nombres);

COMMENT ON TABLE usuarios IS 'Usuarios con acceso a la aplicación';
COMMENT ON COLUMN usuarios.estado IS 'A = activo, I = inactivo';
COMMENT ON COLUMN usuarios.password IS 'Hash BCrypt, nunca el texto plano';

-- ---------------------------------------------------------------------
-- Triggers de auditoría
-- ---------------------------------------------------------------------

-- Al insertar: la fecha de creación la pone la base, sin importar lo que
-- mande la aplicación, y la de modificación queda vacía.
CREATE FUNCTION fn_usuarios_fecha_creacion() RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
    NEW.fecha_creacion := now();
    NEW.fecha_modificacion := NULL;
    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_usuarios_fecha_creacion
    BEFORE INSERT ON usuarios
    FOR EACH ROW
    EXECUTE FUNCTION fn_usuarios_fecha_creacion();

-- Al actualizar: la fecha de creación no se puede reescribir y la de
-- modificación se marca solo si algo cambió de verdad.
CREATE FUNCTION fn_usuarios_fecha_modificacion() RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
    NEW.fecha_creacion := OLD.fecha_creacion;
    NEW.fecha_modificacion := now();
    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_usuarios_fecha_modificacion
    BEFORE UPDATE ON usuarios
    FOR EACH ROW
    WHEN (OLD.* IS DISTINCT FROM NEW.*)
    EXECUTE FUNCTION fn_usuarios_fecha_modificacion();
