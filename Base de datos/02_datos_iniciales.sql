-- =====================================================================
-- Datos iniciales
-- Crear usuarios exige token, así que hace falta al menos uno para entrar.
-- Las credenciales del administrador están en el README.
-- Los hashes se generan aquí con BCrypt (pgcrypto, costo 12), el mismo
-- formato que usa la API, para no dejar hashes opacos en el script.
-- =====================================================================

INSERT INTO usuarios (nombres, apellidos, fecha_nacimiento, direccion, password, telefono, email, estado)
VALUES
    ('Administrador', 'Abank', '1990-01-15', 'San Salvador, El Salvador',
        crypt('Abank2026*', gen_salt('bf', 12)), '70000001', 'admin@abank.test', 'A'),
    ('María José', 'Hernández López', '1993-07-22', 'Col. Escalón, San Salvador',
        crypt('Usuario2026*', gen_salt('bf', 12)), '70000002', 'maria.hernandez@abank.test', 'A'),
    ('Carlos Alberto', 'Martínez Ruiz', '1988-03-17', 'Santa Tecla, La Libertad',
        crypt('Usuario2026*', gen_salt('bf', 12)), '70000003', 'carlos.martinez@abank.test', 'A'),
    ('Ana Sofía', 'Rivas Castillo', '1999-11-05', 'Antiguo Cuscatlán, La Libertad',
        crypt('Usuario2026*', gen_salt('bf', 12)), '70000004', 'ana.rivas@abank.test', 'A'),
    ('José Roberto', 'Flores Méndez', '1985-09-30', 'Soyapango, San Salvador',
        crypt('Usuario2026*', gen_salt('bf', 12)), '70000005', 'jose.flores@abank.test', 'I');
