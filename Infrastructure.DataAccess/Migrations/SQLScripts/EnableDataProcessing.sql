﻿/*
User story reference: 
    https://os2web.atlassian.net/browse/KITOSUDV-1082
 
Content:
    Sets DataProcessing view to be visible on every org in the start.
*/

BEGIN
UPDATE [Config]
SET ShowDataProcessing = 1
END



