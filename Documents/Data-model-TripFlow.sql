CREATE TABLE `Trips` (
  `Id` int PRIMARY KEY AUTO_INCREMENT,
  `Name` varchar(255),
  `Destination` varchar(255),
  `StartDate` date,
  `EndDate` date,
  `Budget` decimal
);

CREATE TABLE `Activities` (
  `Id` int PRIMARY KEY AUTO_INCREMENT,
  `TripId` int,
  `Date` date,
  `Time` time,
  `Title` varchar(255),
  `Location` varchar(255),
  `EstimatedCost` decimal,
  `Note` varchar(255)
);

CREATE TABLE `Expenses` (
  `Id` int PRIMARY KEY AUTO_INCREMENT,
  `TripId` int,
  `Date` date,
  `Amount` decimal,
  `Category` varchar(255),
  `Description` varchar(255)
);

CREATE TABLE `PackingItems` (
  `Id` int PRIMARY KEY AUTO_INCREMENT,
  `TripId` int,
  `Name` varchar(255),
  `Category` varchar(255),
  `IsPacked` boolean
);

ALTER TABLE `Activities` ADD FOREIGN KEY (`TripId`) REFERENCES `Trips` (`Id`);

ALTER TABLE `Expenses` ADD FOREIGN KEY (`TripId`) REFERENCES `Trips` (`Id`);

ALTER TABLE `PackingItems` ADD FOREIGN KEY (`TripId`) REFERENCES `Trips` (`Id`);
