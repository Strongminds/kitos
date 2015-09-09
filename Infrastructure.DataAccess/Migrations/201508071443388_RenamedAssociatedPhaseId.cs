namespace Infrastructure.DataAccess.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class RenamedAssociatedPhaseId : DbMigration
    {
        public override void Up()
        {
            RenameColumn("ItProjectStatus", "AssociatedPhaseId", "AssociatedPhaseNum");

            // recover/convert phase id to phase number
            Sql("UPDATE ItProjectStatus SET AssociatedPhaseNum = 1 WHERE AssociatedPhaseNum IN (" + String.Join(",", _phase1Ids) + ")");
            Sql("UPDATE ItProjectStatus SET AssociatedPhaseNum = 2 WHERE AssociatedPhaseNum IN (" + String.Join(",", _phase2Ids) + ")");
            Sql("UPDATE ItProjectStatus SET AssociatedPhaseNum = 3 WHERE AssociatedPhaseNum IN (" + String.Join(",", _phase3Ids) + ")");
            Sql("UPDATE ItProjectStatus SET AssociatedPhaseNum = 4 WHERE AssociatedPhaseNum IN (" + String.Join(",", _phase4Ids) + ")");
            Sql("UPDATE ItProjectStatus SET AssociatedPhaseNum = 5 WHERE AssociatedPhaseNum IN (" + String.Join(",", _phase5Ids) + ")");
        }

        public override void Down()
        {
            RenameColumn("ItProjectStatus", "AssociatedPhaseNum", "AssociatedPhaseId");
        }

        // these ids are taken from a backup and are only relevant right now, shouldn't be used at a later date
        private readonly int[] _phase1Ids = new[]
        {
            1, 6, 16, 21, 26, 31, 36, 41, 46, 51, 56, 61, 66, 71, 76, 81, 86, 91, 96, 101, 106, 116, 121, 126, 136,
            141, 146, 151, 156, 161, 166, 171, 176, 181, 186, 191, 196, 201, 206, 211, 221, 231, 236, 241, 246, 251,
            256, 261, 266, 271, 281, 286, 291, 296, 301, 306, 311, 326, 331, 336, 341, 346, 351, 356, 366, 371, 376,
            386, 391, 396, 401, 411, 416, 421, 431, 436, 441, 446, 451, 456, 461, 466, 471, 476, 481, 486, 491, 496,
            501, 506, 511, 516, 521, 526, 531, 536, 541, 546, 551, 556, 561, 566, 571, 576, 581, 586, 591, 596, 601,
            606, 611, 616, 621, 626, 631, 636, 641, 646, 651, 656, 661, 666, 671, 676, 681, 686, 691, 696, 701, 706,
            711, 716, 721, 726, 731, 736, 741, 746, 751, 756, 761, 766, 771, 776, 781, 786, 791, 796, 806, 811, 816,
            821, 826, 831, 836, 841, 846, 851, 856, 861, 866, 871, 876, 881, 886, 891, 896, 901, 906, 911, 916, 921,
            926, 931, 936, 941, 946, 951, 956, 961, 966, 971, 976, 981, 986, 991, 996, 1001, 1006, 1011, 1016, 1021,
            1026, 1031, 1041, 1046, 1056, 1071, 1076, 1081, 1086, 1091, 1096, 1101, 1106, 1111, 1116, 1121, 1126,
            1136, 1141, 1146, 1151, 1156, 1161, 1171, 1176, 1181, 1186, 1191, 1196, 1206, 1211, 1216, 1221, 1226,
            1231, 1236, 1241, 1251, 1256, 1261, 1266, 1271, 1276, 1281, 1291, 1301, 1311, 1316, 1321, 1326, 1331,
            1336, 1341, 1346, 1351, 1356, 1361, 1366, 1371, 1376, 1381, 1386, 1396, 1401, 1406, 1411, 1416, 1421,
            1426, 1431, 1441, 1451, 1466, 1471, 1476, 1486, 1491, 1496, 1501, 1506, 1516, 1521, 1526, 1541, 1546,
            1551, 1571, 1581, 1586, 1591, 1596, 1606, 1626, 1631, 1636, 1641, 1656, 1666, 1671, 1676, 1681, 1686,
            1691, 1696, 1701, 1706, 1711, 1716, 1721, 1726, 1731, 1736, 1741, 316, 1651, 1916, 2701
        };

        private readonly int[] _phase2Ids = new[]
        {
            2, 7, 17, 22, 27, 32, 37, 42, 47, 52, 57, 62, 67, 72, 77, 82, 87, 92, 97, 102, 107, 117, 122, 127, 137, 142,
            147, 152, 157, 162, 167, 172, 177, 182, 187, 192, 197, 202, 207, 212, 222, 232, 237, 242, 247, 252, 257,
            262, 267, 272, 282, 287, 292, 297, 302, 307, 312, 327, 332, 337, 342, 347, 352, 357, 367, 372, 377, 387,
            392, 397, 402, 412, 417, 422, 432, 437, 442, 447, 452, 457, 462, 467, 472, 477, 482, 487, 492, 497, 502,
            507, 512, 517, 522, 527, 532, 537, 542, 547, 552, 557, 562, 567, 572, 577, 582, 587, 592, 597, 602, 607,
            612, 617, 622, 627, 632, 637, 642, 647, 652, 657, 662, 667, 672, 677, 682, 687, 692, 697, 702, 707, 712,
            717, 722, 727, 732, 737, 742, 747, 752, 757, 762, 767, 772, 777, 782, 787, 792, 797, 807, 812, 817, 822,
            827, 832, 837, 842, 847, 852, 857, 862, 867, 872, 877, 882, 887, 892, 897, 902, 907, 912, 917, 922, 927,
            932, 937, 942, 947, 952, 957, 962, 967, 972, 977, 982, 987, 992, 997, 1002, 1007, 1012, 1017, 1022, 1027,
            1032, 1042, 1047, 1057, 1072, 1077, 1082, 1087, 1092, 1097, 1102, 1107, 1112, 1117, 1122, 1127, 1137,
            1142, 1147, 1152, 1157, 1162, 1172, 1177, 1182, 1187, 1192, 1197, 1207, 1212, 1217, 1222, 1227, 1232,
            1237, 1242, 1252, 1257, 1262, 1267, 1272, 1277, 1282, 1292, 1302, 1312, 1317, 1322, 1327, 1332, 1337,
            1342, 1347, 1352, 1357, 1362, 1367, 1372, 1382, 1387, 1397, 1402, 1407, 1412, 1417, 1422, 1427, 1432,
            1442, 1452, 1467, 1472, 1477, 1487, 1492, 1497, 1502, 1507, 1517, 1522, 1527, 1542, 1547, 1552, 1572,
            1582, 1587, 1592, 1597, 1607, 1627, 1632, 1637, 1642, 1657, 1667, 1672, 1677, 1682, 1687, 1692, 1697,
            1702, 1707, 1712, 1717, 1722, 1727, 1732, 1737, 1742, 277, 1652, 1787, 1907, 1917, 1977, 2362, 2367, 2397, 2727, 2752
        };

        private readonly int[] _phase3Ids = new[]
        {
            3, 8, 18, 23, 28, 33, 38, 43, 48, 53, 58, 63, 68, 73, 78, 83, 88, 93, 98, 103, 108, 118, 123, 128, 138, 143,
            148, 153, 158, 163, 168, 173, 178, 183, 188, 193, 198, 203, 208, 213, 223, 233, 238, 243, 248, 253, 258,
            263, 268, 273, 283, 288, 293, 298, 303, 308, 313, 328, 333, 338, 343, 348, 353, 358, 368, 373, 378, 388,
            393, 398, 403, 413, 418, 423, 433, 438, 443, 448, 453, 458, 463, 468, 473, 478, 483, 488, 493, 498, 503,
            508, 513, 518, 523, 528, 533, 538, 543, 548, 553, 558, 563, 568, 573, 578, 583, 588, 593, 598, 603, 608,
            613, 618, 623, 628, 633, 638, 643, 648, 653, 658, 663, 668, 673, 678, 683, 688, 693, 698, 703, 708, 713,
            718, 723, 728, 733, 738, 743, 748, 753, 758, 763, 768, 773, 778, 783, 788, 793, 798, 808, 813, 818, 823,
            828, 833, 838, 843, 848, 853, 858, 863, 868, 873, 878, 883, 888, 893, 898, 903, 908, 913, 918, 923, 928,
            933, 938, 943, 948, 953, 958, 963, 968, 973, 978, 983, 988, 993, 998, 1003, 1008, 1013, 1018, 1023, 1028,
            1033, 1043, 1048, 1058, 1073, 1078, 1083, 1088, 1093, 1098, 1103, 1108, 1113, 1118, 1123, 1128, 1138,
            1143, 1148, 1153, 1158, 1163, 1173, 1178, 1183, 1188, 1193, 1198, 1208, 1213, 1218, 1223, 1228, 1233,
            1238, 1243, 1253, 1258, 1263, 1268, 1273, 1278, 1283, 1293, 1303, 1313, 1318, 1323, 1328, 1333, 1338,
            1343, 1348, 1353, 1358, 1363, 1368, 1373, 1383, 1388, 1398, 1403, 1408, 1413, 1418, 1423, 1428, 1433,
            1443, 1453, 1468, 1473, 1478, 1488, 1493, 1498, 1503, 1508, 1518, 1523, 1528, 1543, 1548, 1553, 1573,
            1583, 1588, 1593, 1598, 1608, 1628, 1633, 1638, 1643, 1658, 1668, 1673, 1678, 1683, 1688, 1693, 1698,
            1703, 1708, 1713, 1718, 1723, 1728, 1733, 1738, 1743, 1653, 1788, 1908, 1918, 2238, 2388, 2753
        };

        private readonly int[] _phase4Ids = new[]
        {
            4, 9, 19, 24, 29, 34, 39, 44, 49, 54, 59, 64, 69, 74, 79, 84, 89, 94, 99, 104, 109, 119, 124, 129, 139, 144,
            149, 154, 159, 164, 169, 174, 179, 184, 189, 194, 199, 204, 209, 214, 224, 234, 239, 244, 249, 254, 259,
            264, 269, 274, 284, 289, 294, 299, 304, 309, 314, 329, 334, 339, 344, 349, 354, 359, 369, 374, 379, 389,
            394, 399, 404, 414, 419, 424, 434, 439, 444, 449, 454, 459, 464, 469, 474, 479, 484, 489, 494, 499, 504,
            509, 514, 519, 524, 529, 534, 539, 544, 549, 554, 559, 564, 569, 574, 579, 584, 589, 594, 599, 604, 609,
            614, 619, 624, 629, 634, 639, 644, 649, 654, 659, 664, 669, 674, 679, 684, 689, 694, 699, 704, 709, 714,
            719, 724, 729, 734, 739, 744, 749, 754, 759, 764, 769, 774, 779, 784, 789, 794, 799, 809, 814, 819, 824,
            829, 834, 839, 844, 849, 854, 859, 864, 869, 874, 879, 884, 889, 894, 899, 904, 909, 914, 919, 924, 929,
            934, 939, 944, 949, 954, 959, 964, 969, 974, 979, 984, 989, 994, 999, 1004, 1009, 1014, 1019, 1024, 1029,
            1034, 1044, 1049, 1059, 1074, 1079, 1084, 1089, 1094, 1099, 1104, 1109, 1114, 1119, 1124, 1129, 1139,
            1144, 1149, 1154, 1159, 1164, 1174, 1179, 1184, 1189, 1194, 1199, 1209, 1214, 1219, 1224, 1229, 1234,
            1239, 1244, 1254, 1259, 1264, 1269, 1274, 1279, 1284, 1294, 1304, 1314, 1319, 1324, 1329, 1334, 1339,
            1344, 1349, 1354, 1359, 1364, 1369, 1374, 1384, 1389, 1399, 1404, 1409, 1414, 1419, 1424, 1429, 1434,
            1444, 1454, 1469, 1474, 1479, 1489, 1494, 1499, 1504, 1509, 1519, 1524, 1529, 1544, 1549, 1554, 1574,
            1584, 1589, 1594, 1599, 1609, 1629, 1634, 1639, 1644, 1659, 1669, 1674, 1679, 1684, 1689, 1694, 1699,
            1704, 1709, 1714, 1719, 1724, 1729, 1734, 1739, 1744, 1649, 1654, 1909
        };

        private readonly int[] _phase5Ids = new[]
        {
            5, 10, 20, 25, 30, 35, 40, 45, 50, 55, 60, 65, 70, 75, 80, 85, 90, 95, 100, 105, 110, 120, 125, 130, 140,
            145, 150, 155, 160, 165, 170, 175, 180, 185, 190, 195, 200, 205, 210, 215, 225, 235, 240, 245, 250, 255,
            260, 265, 270, 275, 285, 290, 295, 300, 305, 310, 315, 330, 335, 340, 345, 350, 355, 360, 370, 375, 380,
            390, 395, 400, 405, 415, 420, 425, 435, 440, 445, 450, 455, 460, 465, 470, 475, 480, 485, 490, 495, 500,
            505, 510, 515, 520, 525, 530, 535, 540, 545, 550, 555, 560, 565, 570, 575, 580, 585, 590, 595, 600, 605,
            610, 615, 620, 625, 630, 635, 640, 645, 650, 655, 660, 665, 670, 675, 680, 685, 690, 695, 700, 705, 710,
            715, 720, 725, 730, 735, 740, 745, 750, 755, 760, 765, 770, 775, 780, 785, 790, 795, 800, 810, 815, 820,
            825, 830, 835, 840, 845, 850, 855, 860, 865, 870, 875, 880, 885, 890, 895, 900, 905, 910, 915, 920, 925,
            930, 935, 940, 945, 950, 955, 960, 965, 970, 975, 980, 985, 990, 995, 1000, 1005, 1010, 1015, 1020, 1025,
            1030, 1035, 1045, 1050, 1060, 1075, 1080, 1085, 1090, 1095, 1100, 1105, 1110, 1115, 1120, 1125, 1130,
            1140, 1145, 1150, 1155, 1160, 1165, 1175, 1180, 1185, 1190, 1195, 1200, 1210, 1215, 1220, 1225, 1230,
            1235, 1240, 1245, 1255, 1260, 1265, 1270, 1275, 1280, 1285, 1295, 1305, 1315, 1320, 1325, 1330, 1335,
            1340, 1345, 1350, 1355, 1360, 1365, 1370, 1375, 1380, 1385, 1390, 1400, 1405, 1410, 1415, 1420, 1425,
            1430, 1435, 1445, 1455, 1470, 1475, 1480, 1490, 1495, 1500, 1505, 1510, 1520, 1525, 1530, 1545, 1550,
            1555, 1575, 1585, 1590, 1595, 1600, 1610, 1630, 1635, 1640, 1645, 1660, 1670, 1675, 1680, 1685, 1690,
            1695, 1700, 1705, 1710, 1715, 1720, 1725, 1730, 1735, 1740, 1745, 1650, 1655, 1990
        };
    }
}
