﻿CREATE TABLE `Logs` (
	`Id` BIGINT NOT NULL AUTO_INCREMENT,
	`Exception` TEXT NULL DEFAULT NULL COLLATE 'utf8mb4_unicode_ci',
	`Level` TEXT NULL DEFAULT NULL COLLATE 'utf8mb4_unicode_ci',
	`Message` TEXT NULL DEFAULT NULL COLLATE 'utf8mb4_unicode_ci',
	`MessageTemplate` TEXT NULL DEFAULT NULL COLLATE 'utf8mb4_unicode_ci',
	`Properties` TEXT NULL DEFAULT NULL COLLATE 'utf8mb4_unicode_ci',
	`CreatedAt` DATETIME DEFAULT CURRENT_TIMESTAMP,
	PRIMARY KEY (`Id`,`CreatedAt`) USING BTREE
)
COLLATE='utf8mb4_unicode_ci'
ENGINE=InnoDB
PARTITION BY LIST (MONTH(`CreatedAt`))
(PARTITION Logs_01 VALUES IN (1) ENGINE = InnoDB,
PARTITION Logs_02 VALUES IN (2) ENGINE = InnoDB,
PARTITION Logs_03 VALUES IN (3) ENGINE = InnoDB,
PARTITION Logs_04 VALUES IN (4) ENGINE = InnoDB,
PARTITION Logs_05 VALUES IN (5) ENGINE = InnoDB,
PARTITION Logs_06 VALUES IN (6) ENGINE = InnoDB,
PARTITION Logs_07 VALUES IN (7) ENGINE = InnoDB,
PARTITION Logs_08 VALUES IN (8) ENGINE = InnoDB,
PARTITION Logs_09 VALUES IN (9) ENGINE = InnoDB,
PARTITION Logs_10 VALUES IN (10) ENGINE = InnoDB,
PARTITION Logs_11 VALUES IN (11) ENGINE = InnoDB,
PARTITION Logs_12 VALUES IN (12) ENGINE = InnoDB) ;
;
