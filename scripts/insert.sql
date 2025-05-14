DECLARE @DeviceId NVARCHAR(50);
DECLARE @DeviceName NVARCHAR(100);
DECLARE @IsEnabled BIT;






SET @DeviceId = 'PC-001'; SET @DeviceName = N'Office Mainframe'; SET @IsEnabled = 1;
EXEC AddPersonalComputer @DeviceId, @DeviceName, @IsEnabled, N'Windows Server 2022';

SET @DeviceId = 'PC-002'; SET @DeviceName = N'DevStation Tower'; SET @IsEnabled = 1;
EXEC AddPersonalComputer @DeviceId, @DeviceName, @IsEnabled, N'Fedora Linux';

SET @DeviceId = 'PC-003'; SET @DeviceName = N'Graphics Powerhouse'; SET @IsEnabled = 1;
EXEC AddPersonalComputer @DeviceId, @DeviceName, @IsEnabled, N'Windows 11 Pro for Workstations';

SET @DeviceId = 'PC-004'; SET @DeviceName = N'Compact NUC'; SET @IsEnabled = 1;
EXEC AddPersonalComputer @DeviceId, @DeviceName, @IsEnabled, N'Ubuntu Desktop';

SET @DeviceId = 'PC-005'; SET @DeviceName = N'Reception Kiosk'; SET @IsEnabled = 0;
EXEC AddPersonalComputer @DeviceId, @DeviceName, @IsEnabled, N'Windows 10 IoT';




SET @DeviceId = 'EMB-001'; SET @DeviceName = N'Building Climate Control'; SET @IsEnabled = 1;
EXEC AddEmbedded @DeviceId, @DeviceName, @IsEnabled, '10.1.1.5', 'BMS-Network';

SET @DeviceId = 'EMB-002'; SET @DeviceName = N'Access Point X1'; SET @IsEnabled = 1;
EXEC AddEmbedded @DeviceId, @DeviceName, @IsEnabled, '192.168.0.254', 'CorpWiFi-Main';

SET @DeviceId = 'EMB-003'; SET @DeviceName = N'POS Terminal S1'; SET @IsEnabled = 1;
EXEC AddEmbedded @DeviceId, @DeviceName, @IsEnabled, '172.16.5.10', 'RetailLAN';

SET @DeviceId = 'EMB-004'; SET @DeviceName = N'Digital Signage M1'; SET @IsEnabled = 1;
EXEC AddEmbedded @DeviceId, @DeviceName, @IsEnabled, '192.168.2.33', 'DisplayNet';

SET @DeviceId = 'EMB-005'; SET @DeviceName = N'Lighting Controller L1'; SET @IsEnabled = 0;
EXEC AddEmbedded @DeviceId, @DeviceName, @IsEnabled, '10.5.0.1', 'SmartHome-Lights';


SET @DeviceId = 'SW-001'; SET @DeviceName = N'Sports Watch Pro'; SET @IsEnabled = 1;
EXEC AddSmartwatch @DeviceId, @DeviceName, @IsEnabled, 88;

SET @DeviceId = 'SW-002'; SET @DeviceName = N'Daily Commuter Watch'; SET @IsEnabled = 1;
EXEC AddSmartwatch @DeviceId, @DeviceName, @IsEnabled, 95;

SET @DeviceId = 'SW-003'; SET @DeviceName = N'Luxury Edition Smart'; SET @IsEnabled = 1;
EXEC AddSmartwatch @DeviceId, @DeviceName, @IsEnabled, 70;

SET @DeviceId = 'SW-004'; SET @DeviceName = N'Basic Fitness Band'; SET @IsEnabled = 1;
EXEC AddSmartwatch @DeviceId, @DeviceName, @IsEnabled, 99;

SET @DeviceId = 'SW-005'; SET @DeviceName = N'Kids Geo Watch'; SET @IsEnabled = 0;
EXEC AddSmartwatch @DeviceId, @DeviceName, @IsEnabled, 50;

GO