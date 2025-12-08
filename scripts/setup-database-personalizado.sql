

USE master;
GO

-- Crear base de datos
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'OpinionAnalytics')
BEGIN
    CREATE DATABASE OpinionAnalytics;
    PRINT '✅ Base de datos OpinionAnalytics creada';
END
GO

USE OpinionAnalytics;
GO

-- ========================================
-- DIMENSIÓN: dim_sentiment
-- ========================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'dim_sentiment')
BEGIN
    CREATE TABLE dim_sentiment (
        sentiment_key INT IDENTITY(1,1) PRIMARY KEY,
        sentiment_id NVARCHAR(50) NOT NULL UNIQUE,
        sentiment_name NVARCHAR(100) NOT NULL,
        sentiment_score INT NOT NULL,
        
        -- Otros campos
        score_range_min DECIMAL(5,2) NOT NULL,
        score_range_max DECIMAL(5,2) NOT NULL,
        color_code NVARCHAR(20),
        
        -- Metadata
        created_date DATETIME DEFAULT GETUTCDATE(),
        is_active BIT DEFAULT 1,
        
        INDEX IX_sentiment_score (sentiment_score),
        INDEX IX_is_active (is_active)
    );
    PRINT '✅ Tabla dim_sentiment creada';
END
GO

-- ========================================
-- DIMENSIÓN: dim_source
-- ========================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'dim_source')
BEGIN
    CREATE TABLE dim_source (
        source_key INT IDENTITY(1,1) PRIMARY KEY,
        source_id NVARCHAR(50) NOT NULL UNIQUE,
        source_name NVARCHAR(200) NOT NULL,
        
        -- Características
        source_type NVARCHAR(50) NOT NULL,
        source_category NVARCHAR(100),
        reliability_score DECIMAL(5,2) DEFAULT 1.00,
        
        -- Campo de verificación
        is_active BIT DEFAULT 1,
        
        -- Metadata
        created_date DATETIME DEFAULT GETUTCDATE(),
        updated_date DATETIME NULL,
        
        INDEX IX_source_type (source_type),
        INDEX IX_is_active (is_active)
    );
    PRINT '✅ Tabla dim_source creada';
END
GO

-- ========================================
-- DIMENSIÓN: dim_product
-- ========================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'dim_product')
BEGIN
    CREATE TABLE dim_product (
        product_key INT IDENTITY(1,1) PRIMARY KEY,
        product_id NVARCHAR(50) NOT NULL,
        product_name NVARCHAR(200) NOT NULL,
        
        -- Detalles
        category NVARCHAR(100),
        subcategory NVARCHAR(100),
        price DECIMAL(18,2),
        launch_date DATETIME,
        
        -- Campos de control (SCD Type 2)
        is_active BIT DEFAULT 1,
        effective_date DATETIME DEFAULT GETUTCDATE(),
        end_date DATETIME NULL,
        is_current BIT DEFAULT 1,
        created_date DATETIME DEFAULT GETUTCDATE(),
        
        INDEX IX_product_id (product_id, is_current),
        INDEX IX_category (category),
        INDEX IX_is_current (is_current)
    );
    PRINT '✅ Tabla dim_product creada';
END
GO

-- ========================================
-- DIMENSIÓN: dim_client
-- ========================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'dim_client')
BEGIN
    CREATE TABLE dim_client (
        client_key INT IDENTITY(1,1) PRIMARY KEY,
        client_id NVARCHAR(50) NOT NULL UNIQUE,
        client_name NVARCHAR(200) NOT NULL,
        
        -- Atributos demográficos
        country NVARCHAR(100),
        age INT,
        gender NVARCHAR(20),
        client_type NVARCHAR(50),
        
        -- Otros datos
        registration_date DATETIME,
        client_segment NVARCHAR(50),
        created_date DATETIME DEFAULT GETUTCDATE(),
        
        -- Control
        is_active BIT DEFAULT 1,
        
        INDEX IX_client_id (client_id),
        INDEX IX_country (country),
        INDEX IX_client_type (client_type)
    );
    PRINT '✅ Tabla dim_client creada';
END
GO

-- ========================================
-- DIMENSIÓN: dim_time
-- ========================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'dim_time')
BEGIN
    CREATE TABLE dim_time (
        time_key INT PRIMARY KEY,
        full_date DATE NOT NULL UNIQUE,
        
        -- Jerarquías temporales
        [day] INT NOT NULL,
        [week] INT NOT NULL,
        [month] INT NOT NULL,
        [quarter] INT NOT NULL,
        [year] INT NOT NULL,
        
        -- Nombres descriptivos
        day_name NVARCHAR(20),
        month_name NVARCHAR(20),
        quarter_name NVARCHAR(10),
        
        -- Indicadores especiales
        is_weekend BIT DEFAULT 0,
        is_holiday BIT DEFAULT 0,
        holiday_name NVARCHAR(100),
        is_business_day BIT DEFAULT 1,
        
        -- Metadata
        created_date DATETIME DEFAULT GETUTCDATE(),
        
        INDEX IX_full_date (full_date),
        INDEX IX_year_month ([year], [month]),
        INDEX IX_is_weekend (is_weekend)
    );
    PRINT '✅ Tabla dim_time creada';
END
GO

-- ========================================
-- TABLA DE HECHOS: fact_opinions
-- ========================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'fact_opinions')
BEGIN
    CREATE TABLE fact_opinions (
        opinion_key BIGINT IDENTITY(1,1) PRIMARY KEY,
        
        -- Claves foráneas (conexiones)
        client_key INT NOT NULL,
        product_key INT NOT NULL,
        time_key INT NOT NULL,
        source_key INT NOT NULL,
        sentiment_key INT NOT NULL,
        
        -- Métricas y datos principales
        rating DECIMAL(3,1),
        sentiment_score DECIMAL(5,2) NOT NULL,
        comment_text NVARCHAR(MAX) NOT NULL,
        comment_length INT NOT NULL,
        word_count INT NOT NULL,
        
        -- Metadatos
        contains_keywords BIT DEFAULT 0,
        response_time_hours INT,
        original_comment_id NVARCHAR(100),
        comment_type NVARCHAR(50),
        channel_code NVARCHAR(50),
        
        -- Auditoría
        created_date DATETIME DEFAULT GETUTCDATE(),
        updated_date DATETIME NULL,
        etl_batch_id NVARCHAR(100),
        
        -- Relaciones (Foreign Keys)
        CONSTRAINT FK_fact_opinions_client 
            FOREIGN KEY (client_key) REFERENCES dim_client(client_key),
        CONSTRAINT FK_fact_opinions_product 
            FOREIGN KEY (product_key) REFERENCES dim_product(product_key),
        CONSTRAINT FK_fact_opinions_time 
            FOREIGN KEY (time_key) REFERENCES dim_time(time_key),
        CONSTRAINT FK_fact_opinions_source 
            FOREIGN KEY (source_key) REFERENCES dim_source(source_key),
        CONSTRAINT FK_fact_opinions_sentiment 
            FOREIGN KEY (sentiment_key) REFERENCES dim_sentiment(sentiment_key),
        
        -- Índices para mejorar rendimiento
        INDEX IX_client_key (client_key),
        INDEX IX_product_key (product_key),
        INDEX IX_time_key (time_key),
        INDEX IX_source_key (source_key),
        INDEX IX_sentiment_key (sentiment_key),
        INDEX IX_created_date (created_date),
        INDEX IX_rating (rating)
    );
    PRINT '✅ Tabla fact_opinions creada';
END
GO

-- ========================================
-- DATOS INICIALES
-- ========================================

-- Insertar sentimientos predefinidos
IF NOT EXISTS (SELECT * FROM dim_sentiment)
BEGIN
    INSERT INTO dim_sentiment (sentiment_id, sentiment_name, sentiment_score, score_range_min, score_range_max, color_code)
    VALUES
    ('1', 'Muy Negativo', 1, -1.00, -0.60, '#DC3545'),
    ('2', 'Negativo', 2, -0.60, -0.20, '#FD7E14'),
    ('3', 'Neutral', 3, -0.20, 0.20, '#FFC107'),
    ('4', 'Positivo', 4, 0.20, 0.60, '#28A745'),
    ('5', 'Muy Positivo', 5, 0.60, 1.00, '#198754');
    
    PRINT '✅ Sentimientos iniciales insertados';
END
GO

-- Insertar registros "Desconocidos" para integridad referencial
IF NOT EXISTS (SELECT * FROM dim_client WHERE client_key = 1)
BEGIN
    SET IDENTITY_INSERT dim_client ON;
    INSERT INTO dim_client (client_key, client_id, client_name, country, client_type, registration_date)
    VALUES (1, 'UNKNOWN', 'Cliente Desconocido', 'Unknown', 'Unknown', GETUTCDATE());
    SET IDENTITY_INSERT dim_client OFF;
    PRINT '✅ Cliente "Desconocido" insertado';
END
GO

IF NOT EXISTS (SELECT * FROM dim_product WHERE product_key = 1)
BEGIN
    SET IDENTITY_INSERT dim_product ON;
    INSERT INTO dim_product (product_key, product_id, product_name, category, is_current, effective_date)
    VALUES (1, 'UNKNOWN', 'Producto Desconocido', 'Unknown', 1, GETUTCDATE());
    SET IDENTITY_INSERT dim_product OFF;
    PRINT '✅ Producto "Desconocido" insertado';
END
GO

IF NOT EXISTS (SELECT * FROM dim_source WHERE source_key = 1)
BEGIN
    SET IDENTITY_INSERT dim_source ON;
    INSERT INTO dim_source (source_key, source_id, source_name, source_type, is_active)
    VALUES (1, 'UNKNOWN', 'Fuente Desconocida', 'Unknown', 1);
    SET IDENTITY_INSERT dim_source OFF;
    PRINT '✅ Fuente "Desconocida" insertada';
END
GO

-- ========================================
-- VISTAS ANALÍTICAS
-- ========================================

-- Vista principal de análisis de opiniones
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_opinions_analysis')
    DROP VIEW vw_opinions_analysis;
GO

CREATE VIEW vw_opinions_analysis AS
SELECT 
    f.opinion_key,
    f.comment_text,
    f.rating,
    f.sentiment_score,
    f.word_count,
    f.comment_length,
    f.created_date,
    
    -- Cliente
    c.client_id,
    c.client_name,
    c.country,
    c.age,
    c.gender,
    c.client_type,
    c.client_segment,
    
    -- Producto
    p.product_id,
    p.product_name,
    p.category,
    p.subcategory,
    p.price,
    
    -- Tiempo
    t.full_date,
    t.[year],
    t.[quarter],
    t.[month],
    t.month_name,
    t.day_name,
    t.is_weekend,
    t.is_holiday,
    
    -- Fuente
    s.source_name,
    s.source_type,
    s.source_category,
    s.reliability_score,
    
    -- Sentimiento
    st.sentiment_name,
    st.sentiment_score AS sentiment_category,
    st.color_code,
    
    -- Metadatos
    f.contains_keywords,
    f.response_time_hours,
    f.comment_type,
    f.channel_code
FROM fact_opinions f
INNER JOIN dim_client c ON f.client_key = c.client_key
INNER JOIN dim_product p ON f.product_key = p.product_key
INNER JOIN dim_time t ON f.time_key = t.time_key
INNER JOIN dim_source s ON f.source_key = s.source_key
INNER JOIN dim_sentiment st ON f.sentiment_key = st.sentiment_key;
GO

PRINT '✅ Vista vw_opinions_analysis creada';
GO

-- Vista de resumen por producto
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_product_summary')
    DROP VIEW vw_product_summary;
GO

CREATE VIEW vw_product_summary AS
SELECT 
    p.product_id,
    p.product_name,
    p.category,
    p.subcategory,
    COUNT(f.opinion_key) as total_opinions,
    AVG(CAST(f.rating AS FLOAT)) as avg_rating,
    AVG(f.sentiment_score) as avg_sentiment_score,
    SUM(CASE WHEN st.sentiment_score >= 4 THEN 1 ELSE 0 END) as positive_opinions,
    SUM(CASE WHEN st.sentiment_score <= 2 THEN 1 ELSE 0 END) as negative_opinions,
    SUM(CASE WHEN st.sentiment_score = 3 THEN 1 ELSE 0 END) as neutral_opinions,
    MIN(f.created_date) as first_opinion_date,
    MAX(f.created_date) as last_opinion_date
FROM dim_product p
LEFT JOIN fact_opinions f ON p.product_key = f.product_key
LEFT JOIN dim_sentiment st ON f.sentiment_key = st.sentiment_key
WHERE p.is_current = 1
GROUP BY p.product_id, p.product_name, p.category, p.subcategory;
GO

PRINT '✅ Vista vw_product_summary creada';
GO

-- Vista de resumen por fuente
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_source_summary')
    DROP VIEW vw_source_summary;
GO

CREATE VIEW vw_source_summary AS
SELECT 
    s.source_name,
    s.source_type,
    s.source_category,
    COUNT(f.opinion_key) as total_opinions,
    AVG(CAST(f.rating AS FLOAT)) as avg_rating,
    AVG(f.sentiment_score) as avg_sentiment_score,
    AVG(CAST(f.word_count AS FLOAT)) as avg_word_count,
    MIN(f.created_date) as first_opinion_date,
    MAX(f.created_date) as last_opinion_date
FROM dim_source s
LEFT JOIN fact_opinions f ON s.source_key = f.source_key
WHERE s.is_active = 1
GROUP BY s.source_name, s.source_type, s.source_category;
GO

PRINT '✅ Vista vw_source_summary creada';
GO

-- ========================================
-- PROCEDIMIENTOS ALMACENADOS
-- ========================================

-- Procedimiento: Obtener opiniones por rango de fechas
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_get_opinions_by_date_range')
    DROP PROCEDURE sp_get_opinions_by_date_range;
GO

CREATE PROCEDURE sp_get_opinions_by_date_range
    @start_date DATE,
    @end_date DATE
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT *
    FROM vw_opinions_analysis
    WHERE full_date BETWEEN @start_date AND @end_date
    ORDER BY full_date DESC, created_date DESC;
END
GO

PRINT '✅ Procedimiento sp_get_opinions_by_date_range creado';
GO

-- Procedimiento: Obtener top productos por sentimiento
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_get_top_products_by_sentiment')
    DROP PROCEDURE sp_get_top_products_by_sentiment;
GO

CREATE PROCEDURE sp_get_top_products_by_sentiment
    @sentiment_type NVARCHAR(20), -- 'positive' o 'negative'
    @top_n INT = 10
AS
BEGIN
    SET NOCOUNT ON;
    
    IF @sentiment_type = 'positive'
    BEGIN
        SELECT TOP (@top_n)
            product_id,
            product_name,
            category,
            total_opinions,
            avg_rating,
            positive_opinions,
            CAST(positive_opinions AS FLOAT) / NULLIF(total_opinions, 0) * 100 as positive_percentage
        FROM vw_product_summary
        WHERE total_opinions > 0
        ORDER BY positive_opinions DESC, avg_rating DESC;
    END
    ELSE IF @sentiment_type = 'negative'
    BEGIN
        SELECT TOP (@top_n)
            product_id,
            product_name,
            category,
            total_opinions,
            avg_rating,
            negative_opinions,
            CAST(negative_opinions AS FLOAT) / NULLIF(total_opinions, 0) * 100 as negative_percentage
        FROM vw_product_summary
        WHERE total_opinions > 0
        ORDER BY negative_opinions DESC, avg_rating ASC;
    END
END
GO

PRINT '✅ Procedimiento sp_get_top_products_by_sentiment creado';
GO

-- ========================================
-- FUNCIÓN: Calcular DateKey (YYYYMMDD)
-- ========================================
IF EXISTS (SELECT * FROM sys.objects WHERE name = 'fn_calculate_time_key')
    DROP FUNCTION fn_calculate_time_key;
GO

CREATE FUNCTION fn_calculate_time_key(@date DATE)
RETURNS INT
AS
BEGIN
    RETURN YEAR(@date) * 10000 + MONTH(@date) * 100 + DAY(@date);
END
GO

PRINT '✅ Función fn_calculate_time_key creada';
GO

-- ========================================
-- FINALIZACIÓN
-- ========================================

PRINT '';
PRINT '========================================';
PRINT '✅ BASE DE DATOS ANALÍTICA CREADA';
PRINT '========================================';
PRINT '';
PRINT '📊 TABLAS CREADAS:';
PRINT '  ✓ dim_sentiment (Dimensión de Sentimiento)';
PRINT '  ✓ dim_source (Dimensión de Fuente)';
PRINT '  ✓ dim_product (Dimensión de Producto)';
PRINT '  ✓ dim_client (Dimensión de Cliente)';
PRINT '  ✓ dim_time (Dimensión de Tiempo)';
PRINT '  ✓ fact_opinions (Tabla de Hechos)';
PRINT '';
PRINT '📈 VISTAS ANALÍTICAS:';
PRINT '  ✓ vw_opinions_analysis';
PRINT '  ✓ vw_product_summary';
PRINT '  ✓ vw_source_summary';
PRINT '';
PRINT '⚙️  PROCEDIMIENTOS:';
PRINT '  ✓ sp_get_opinions_by_date_range';
PRINT '  ✓ sp_get_top_products_by_sentiment';
PRINT '';
PRINT '🔧 FUNCIONES:';
PRINT '  ✓ fn_calculate_time_key';
PRINT '';
PRINT '🎉 Sistema listo para carga de datos!';
PRINT '========================================';
GO