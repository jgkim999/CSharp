-- mydb 데이터베이스 생성
CREATE DATABASE IF NOT EXISTS mydb CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

USE mydb;

-- 예제 테이블 생성
CREATE TABLE IF NOT EXISTS users (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserName VARCHAR(50) NOT NULL UNIQUE,
    UserPassword VARCHAR(64) NOT NULL,
    Email VARCHAR(100) NOT NULL,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS products (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Description TEXT,
    Price DECIMAL(10, 2) NOT NULL,
    Stock INT DEFAULT 0,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 초기 데이터 삽입 (선택사항), 1234
INSERT INTO users (UserName, Email, UserPassword) VALUES 
    ('admin', 'admin@example.com', '03ac674216f3e15c761ee1a5e255f067953623c8b388b4459e13f978d7c846f4'),
    ('user1', 'user1@example.com', '03ac674216f3e15c761ee1a5e255f067953623c8b388b4459e13f978d7c846f4')
ON DUPLICATE KEY UPDATE UserName=UserName;

INSERT INTO products (Name, Description, Price, Stock) VALUES 
    ('Product 1', 'Description for product 1', 29.99, 100),
    ('Product 2', 'Description for product 2', 49.99, 50)
ON DUPLICATE KEY UPDATE Name=Name;
