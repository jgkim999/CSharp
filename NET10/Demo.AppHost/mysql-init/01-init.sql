-- mydb 데이터베이스 생성
CREATE DATABASE IF NOT EXISTS mydb CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

USE mydb;

-- 예제 테이블 생성
CREATE TABLE IF NOT EXISTS users
(
    user_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    user_name VARCHAR(50) NOT NULL UNIQUE,
    user_password VARCHAR(64) NOT NULL,
    user_email VARCHAR(100) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS companies
(
    company_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    company_name VARCHAR(50) NOT NULL UNIQUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS products
(
    product_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    company_id BIGINT NOT NULL,
    product_name VARCHAR(100) NOT NULL,
    product_description TEXT,
    product_price DECIMAL(10, 2) NOT NULL,
    product_stock INT NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (company_id) REFERENCES companies(company_id)
);

CREATE TABLE IF NOT EXISTS orders
(
    order_id       BIGINT AUTO_INCREMENT PRIMARY KEY,
    user_id        BIGINT         NOT NULL,
    product_id     BIGINT         NOT NULL,
    order_quantity INT            NOT NULL,
    order_status   VARCHAR(20)    NOT NULL,
    created_at     TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at     TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users (user_id),
    FOREIGN KEY (product_id) REFERENCES products (product_id) 
);

-- 초기 데이터 삽입 (선택사항), 1234
INSERT INTO users (user_name, user_email, user_password) VALUES 
    ('admin', 'admin@example.com', '03ac674216f3e15c761ee1a5e255f067953623c8b388b4459e13f978d7c846f4'),
    ('user1', 'user1@example.com', '03ac674216f3e15c761ee1a5e255f067953623c8b388b4459e13f978d7c846f4')
ON DUPLICATE KEY UPDATE UserName=UserName;

INSERT INTO products (product_name, product_description, product_price, product_stock) VALUES 
    ('Product 1', 'Description for product 1', 29.99, 100),
    ('Product 2', 'Description for product 2', 49.99, 50)
ON DUPLICATE KEY UPDATE Name=Name;
