CREATE DATABASE IF NOT EXISTS mydb;

USE mydb;


CREATE TABLE `users` (
    `id` bigint NOT NULL AUTO_INCREMENT,
    `name` varchar(255) NOT NULL,
    `email` varchar(255) NOT NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `users_name_IDX` (`name`) USING BTREE,
    UNIQUE KEY `users_email_IDX` (`email`) USING BTREE
);
