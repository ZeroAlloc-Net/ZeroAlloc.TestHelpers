using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ZeroAlloc.TestHelpers.Tests;

/// <summary>
/// Other threads calling methods for the first time, as the other tests of a large test run do.
/// The runtime waits for tiering until no method has been called for the first time for 100 ms,
/// so a steady trickle of new methods holds it back, and lets it through in the gaps (#94). Each
/// thread calls the next of a pool of distinct methods, 35 times so that it counts as hot, then
/// sleeps for a random time up to the given gap. A start uses methods no earlier start used.
/// </summary>
internal sealed class TieringNoise : IDisposable
{
    private const int Threads = 4;

    private static int s_next;

    private readonly CancellationTokenSource _stop = new();
    private readonly Thread[] _threads = new Thread[Threads];

    private TieringNoise(int maxGapMilliseconds)
    {
        var first = Interlocked.Add(ref s_next, Methods.Length / 8) - (Methods.Length / 8);
        if (first + (Methods.Length / 8) > Methods.Length)
            throw new InvalidOperationException("TieringNoise ran out of methods; add more to the pool.");

        for (var t = 0; t < Threads; t++)
        {
            var from = first + (t * (Methods.Length / (8 * Threads)));
            var to = from + (Methods.Length / (8 * Threads));
            var random = new Random(t);
            _threads[t] = new Thread(() => Run(from, to, maxGapMilliseconds, random)) { IsBackground = true };
            _threads[t].Start();
        }
    }

    public static TieringNoise Start(int maxGapMilliseconds) => new(maxGapMilliseconds);

    public void Dispose()
    {
        _stop.Cancel();
        foreach (var thread in _threads)
            thread.Join();
        _stop.Dispose();
    }

    private void Run(int from, int to, int maxGapMilliseconds, Random random)
    {
        var sum = 0;
        for (var i = from; i < to && !_stop.IsCancellationRequested; i++)
        {
            for (var k = 0; k < 35; k++)
                sum += Methods[i](k);

            _ = _stop.Token.WaitHandle.WaitOne(random.Next(0, maxGapMilliseconds));
        }

        Escape.Sink = sum;
    }

    private static readonly Func<int, int>[] Methods =
    [
        N0,
        N1,
        N2,
        N3,
        N4,
        N5,
        N6,
        N7,
        N8,
        N9,
        N10,
        N11,
        N12,
        N13,
        N14,
        N15,
        N16,
        N17,
        N18,
        N19,
        N20,
        N21,
        N22,
        N23,
        N24,
        N25,
        N26,
        N27,
        N28,
        N29,
        N30,
        N31,
        N32,
        N33,
        N34,
        N35,
        N36,
        N37,
        N38,
        N39,
        N40,
        N41,
        N42,
        N43,
        N44,
        N45,
        N46,
        N47,
        N48,
        N49,
        N50,
        N51,
        N52,
        N53,
        N54,
        N55,
        N56,
        N57,
        N58,
        N59,
        N60,
        N61,
        N62,
        N63,
        N64,
        N65,
        N66,
        N67,
        N68,
        N69,
        N70,
        N71,
        N72,
        N73,
        N74,
        N75,
        N76,
        N77,
        N78,
        N79,
        N80,
        N81,
        N82,
        N83,
        N84,
        N85,
        N86,
        N87,
        N88,
        N89,
        N90,
        N91,
        N92,
        N93,
        N94,
        N95,
        N96,
        N97,
        N98,
        N99,
        N100,
        N101,
        N102,
        N103,
        N104,
        N105,
        N106,
        N107,
        N108,
        N109,
        N110,
        N111,
        N112,
        N113,
        N114,
        N115,
        N116,
        N117,
        N118,
        N119,
        N120,
        N121,
        N122,
        N123,
        N124,
        N125,
        N126,
        N127,
        N128,
        N129,
        N130,
        N131,
        N132,
        N133,
        N134,
        N135,
        N136,
        N137,
        N138,
        N139,
        N140,
        N141,
        N142,
        N143,
        N144,
        N145,
        N146,
        N147,
        N148,
        N149,
        N150,
        N151,
        N152,
        N153,
        N154,
        N155,
        N156,
        N157,
        N158,
        N159,
        N160,
        N161,
        N162,
        N163,
        N164,
        N165,
        N166,
        N167,
        N168,
        N169,
        N170,
        N171,
        N172,
        N173,
        N174,
        N175,
        N176,
        N177,
        N178,
        N179,
        N180,
        N181,
        N182,
        N183,
        N184,
        N185,
        N186,
        N187,
        N188,
        N189,
        N190,
        N191,
        N192,
        N193,
        N194,
        N195,
        N196,
        N197,
        N198,
        N199,
        N200,
        N201,
        N202,
        N203,
        N204,
        N205,
        N206,
        N207,
        N208,
        N209,
        N210,
        N211,
        N212,
        N213,
        N214,
        N215,
        N216,
        N217,
        N218,
        N219,
        N220,
        N221,
        N222,
        N223,
        N224,
        N225,
        N226,
        N227,
        N228,
        N229,
        N230,
        N231,
        N232,
        N233,
        N234,
        N235,
        N236,
        N237,
        N238,
        N239,
        N240,
        N241,
        N242,
        N243,
        N244,
        N245,
        N246,
        N247,
        N248,
        N249,
        N250,
        N251,
        N252,
        N253,
        N254,
        N255,
        N256,
        N257,
        N258,
        N259,
        N260,
        N261,
        N262,
        N263,
        N264,
        N265,
        N266,
        N267,
        N268,
        N269,
        N270,
        N271,
        N272,
        N273,
        N274,
        N275,
        N276,
        N277,
        N278,
        N279,
        N280,
        N281,
        N282,
        N283,
        N284,
        N285,
        N286,
        N287,
        N288,
        N289,
        N290,
        N291,
        N292,
        N293,
        N294,
        N295,
        N296,
        N297,
        N298,
        N299,
        N300,
        N301,
        N302,
        N303,
        N304,
        N305,
        N306,
        N307,
        N308,
        N309,
        N310,
        N311,
        N312,
        N313,
        N314,
        N315,
        N316,
        N317,
        N318,
        N319,
        N320,
        N321,
        N322,
        N323,
        N324,
        N325,
        N326,
        N327,
        N328,
        N329,
        N330,
        N331,
        N332,
        N333,
        N334,
        N335,
        N336,
        N337,
        N338,
        N339,
        N340,
        N341,
        N342,
        N343,
        N344,
        N345,
        N346,
        N347,
        N348,
        N349,
        N350,
        N351,
        N352,
        N353,
        N354,
        N355,
        N356,
        N357,
        N358,
        N359,
        N360,
        N361,
        N362,
        N363,
        N364,
        N365,
        N366,
        N367,
        N368,
        N369,
        N370,
        N371,
        N372,
        N373,
        N374,
        N375,
        N376,
        N377,
        N378,
        N379,
        N380,
        N381,
        N382,
        N383,
        N384,
        N385,
        N386,
        N387,
        N388,
        N389,
        N390,
        N391,
        N392,
        N393,
        N394,
        N395,
        N396,
        N397,
        N398,
        N399,
        N400,
        N401,
        N402,
        N403,
        N404,
        N405,
        N406,
        N407,
        N408,
        N409,
        N410,
        N411,
        N412,
        N413,
        N414,
        N415,
        N416,
        N417,
        N418,
        N419,
        N420,
        N421,
        N422,
        N423,
        N424,
        N425,
        N426,
        N427,
        N428,
        N429,
        N430,
        N431,
        N432,
        N433,
        N434,
        N435,
        N436,
        N437,
        N438,
        N439,
        N440,
        N441,
        N442,
        N443,
        N444,
        N445,
        N446,
        N447,
        N448,
        N449,
        N450,
        N451,
        N452,
        N453,
        N454,
        N455,
        N456,
        N457,
        N458,
        N459,
        N460,
        N461,
        N462,
        N463,
        N464,
        N465,
        N466,
        N467,
        N468,
        N469,
        N470,
        N471,
        N472,
        N473,
        N474,
        N475,
        N476,
        N477,
        N478,
        N479,
        N480,
        N481,
        N482,
        N483,
        N484,
        N485,
        N486,
        N487,
        N488,
        N489,
        N490,
        N491,
        N492,
        N493,
        N494,
        N495,
        N496,
        N497,
        N498,
        N499,
        N500,
        N501,
        N502,
        N503,
        N504,
        N505,
        N506,
        N507,
        N508,
        N509,
        N510,
        N511,
        N512,
        N513,
        N514,
        N515,
        N516,
        N517,
        N518,
        N519,
        N520,
        N521,
        N522,
        N523,
        N524,
        N525,
        N526,
        N527,
        N528,
        N529,
        N530,
        N531,
        N532,
        N533,
        N534,
        N535,
        N536,
        N537,
        N538,
        N539,
        N540,
        N541,
        N542,
        N543,
        N544,
        N545,
        N546,
        N547,
        N548,
        N549,
        N550,
        N551,
        N552,
        N553,
        N554,
        N555,
        N556,
        N557,
        N558,
        N559,
        N560,
        N561,
        N562,
        N563,
        N564,
        N565,
        N566,
        N567,
        N568,
        N569,
        N570,
        N571,
        N572,
        N573,
        N574,
        N575,
        N576,
        N577,
        N578,
        N579,
        N580,
        N581,
        N582,
        N583,
        N584,
        N585,
        N586,
        N587,
        N588,
        N589,
        N590,
        N591,
        N592,
        N593,
        N594,
        N595,
        N596,
        N597,
        N598,
        N599,
        N600,
        N601,
        N602,
        N603,
        N604,
        N605,
        N606,
        N607,
        N608,
        N609,
        N610,
        N611,
        N612,
        N613,
        N614,
        N615,
        N616,
        N617,
        N618,
        N619,
        N620,
        N621,
        N622,
        N623,
        N624,
        N625,
        N626,
        N627,
        N628,
        N629,
        N630,
        N631,
        N632,
        N633,
        N634,
        N635,
        N636,
        N637,
        N638,
        N639,
        N640,
        N641,
        N642,
        N643,
        N644,
        N645,
        N646,
        N647,
        N648,
        N649,
        N650,
        N651,
        N652,
        N653,
        N654,
        N655,
        N656,
        N657,
        N658,
        N659,
        N660,
        N661,
        N662,
        N663,
        N664,
        N665,
        N666,
        N667,
        N668,
        N669,
        N670,
        N671,
        N672,
        N673,
        N674,
        N675,
        N676,
        N677,
        N678,
        N679,
        N680,
        N681,
        N682,
        N683,
        N684,
        N685,
        N686,
        N687,
        N688,
        N689,
        N690,
        N691,
        N692,
        N693,
        N694,
        N695,
        N696,
        N697,
        N698,
        N699,
        N700,
        N701,
        N702,
        N703,
        N704,
        N705,
        N706,
        N707,
        N708,
        N709,
        N710,
        N711,
        N712,
        N713,
        N714,
        N715,
        N716,
        N717,
        N718,
        N719,
        N720,
        N721,
        N722,
        N723,
        N724,
        N725,
        N726,
        N727,
        N728,
        N729,
        N730,
        N731,
        N732,
        N733,
        N734,
        N735,
        N736,
        N737,
        N738,
        N739,
        N740,
        N741,
        N742,
        N743,
        N744,
        N745,
        N746,
        N747,
        N748,
        N749,
        N750,
        N751,
        N752,
        N753,
        N754,
        N755,
        N756,
        N757,
        N758,
        N759,
        N760,
        N761,
        N762,
        N763,
        N764,
        N765,
        N766,
        N767,
        N768,
        N769,
        N770,
        N771,
        N772,
        N773,
        N774,
        N775,
        N776,
        N777,
        N778,
        N779,
        N780,
        N781,
        N782,
        N783,
        N784,
        N785,
        N786,
        N787,
        N788,
        N789,
        N790,
        N791,
        N792,
        N793,
        N794,
        N795,
        N796,
        N797,
        N798,
        N799,
        N800,
        N801,
        N802,
        N803,
        N804,
        N805,
        N806,
        N807,
        N808,
        N809,
        N810,
        N811,
        N812,
        N813,
        N814,
        N815,
        N816,
        N817,
        N818,
        N819,
        N820,
        N821,
        N822,
        N823,
        N824,
        N825,
        N826,
        N827,
        N828,
        N829,
        N830,
        N831,
        N832,
        N833,
        N834,
        N835,
        N836,
        N837,
        N838,
        N839,
        N840,
        N841,
        N842,
        N843,
        N844,
        N845,
        N846,
        N847,
        N848,
        N849,
        N850,
        N851,
        N852,
        N853,
        N854,
        N855,
        N856,
        N857,
        N858,
        N859,
        N860,
        N861,
        N862,
        N863,
        N864,
        N865,
        N866,
        N867,
        N868,
        N869,
        N870,
        N871,
        N872,
        N873,
        N874,
        N875,
        N876,
        N877,
        N878,
        N879,
        N880,
        N881,
        N882,
        N883,
        N884,
        N885,
        N886,
        N887,
        N888,
        N889,
        N890,
        N891,
        N892,
        N893,
        N894,
        N895,
        N896,
        N897,
        N898,
        N899,
        N900,
        N901,
        N902,
        N903,
        N904,
        N905,
        N906,
        N907,
        N908,
        N909,
        N910,
        N911,
        N912,
        N913,
        N914,
        N915,
        N916,
        N917,
        N918,
        N919,
        N920,
        N921,
        N922,
        N923,
        N924,
        N925,
        N926,
        N927,
        N928,
        N929,
        N930,
        N931,
        N932,
        N933,
        N934,
        N935,
        N936,
        N937,
        N938,
        N939,
        N940,
        N941,
        N942,
        N943,
        N944,
        N945,
        N946,
        N947,
        N948,
        N949,
        N950,
        N951,
        N952,
        N953,
        N954,
        N955,
        N956,
        N957,
        N958,
        N959,
        N960,
        N961,
        N962,
        N963,
        N964,
        N965,
        N966,
        N967,
        N968,
        N969,
        N970,
        N971,
        N972,
        N973,
        N974,
        N975,
        N976,
        N977,
        N978,
        N979,
        N980,
        N981,
        N982,
        N983,
        N984,
        N985,
        N986,
        N987,
        N988,
        N989,
        N990,
        N991,
        N992,
        N993,
        N994,
        N995,
        N996,
        N997,
        N998,
        N999,
        N1000,
        N1001,
        N1002,
        N1003,
        N1004,
        N1005,
        N1006,
        N1007,
        N1008,
        N1009,
        N1010,
        N1011,
        N1012,
        N1013,
        N1014,
        N1015,
        N1016,
        N1017,
        N1018,
        N1019,
        N1020,
        N1021,
        N1022,
        N1023,
        N1024,
        N1025,
        N1026,
        N1027,
        N1028,
        N1029,
        N1030,
        N1031,
        N1032,
        N1033,
        N1034,
        N1035,
        N1036,
        N1037,
        N1038,
        N1039,
        N1040,
        N1041,
        N1042,
        N1043,
        N1044,
        N1045,
        N1046,
        N1047,
        N1048,
        N1049,
        N1050,
        N1051,
        N1052,
        N1053,
        N1054,
        N1055,
        N1056,
        N1057,
        N1058,
        N1059,
        N1060,
        N1061,
        N1062,
        N1063,
        N1064,
        N1065,
        N1066,
        N1067,
        N1068,
        N1069,
        N1070,
        N1071,
        N1072,
        N1073,
        N1074,
        N1075,
        N1076,
        N1077,
        N1078,
        N1079,
        N1080,
        N1081,
        N1082,
        N1083,
        N1084,
        N1085,
        N1086,
        N1087,
        N1088,
        N1089,
        N1090,
        N1091,
        N1092,
        N1093,
        N1094,
        N1095,
        N1096,
        N1097,
        N1098,
        N1099,
        N1100,
        N1101,
        N1102,
        N1103,
        N1104,
        N1105,
        N1106,
        N1107,
        N1108,
        N1109,
        N1110,
        N1111,
        N1112,
        N1113,
        N1114,
        N1115,
        N1116,
        N1117,
        N1118,
        N1119,
        N1120,
        N1121,
        N1122,
        N1123,
        N1124,
        N1125,
        N1126,
        N1127,
        N1128,
        N1129,
        N1130,
        N1131,
        N1132,
        N1133,
        N1134,
        N1135,
        N1136,
        N1137,
        N1138,
        N1139,
        N1140,
        N1141,
        N1142,
        N1143,
        N1144,
        N1145,
        N1146,
        N1147,
        N1148,
        N1149,
        N1150,
        N1151,
        N1152,
        N1153,
        N1154,
        N1155,
        N1156,
        N1157,
        N1158,
        N1159,
        N1160,
        N1161,
        N1162,
        N1163,
        N1164,
        N1165,
        N1166,
        N1167,
        N1168,
        N1169,
        N1170,
        N1171,
        N1172,
        N1173,
        N1174,
        N1175,
        N1176,
        N1177,
        N1178,
        N1179,
        N1180,
        N1181,
        N1182,
        N1183,
        N1184,
        N1185,
        N1186,
        N1187,
        N1188,
        N1189,
        N1190,
        N1191,
        N1192,
        N1193,
        N1194,
        N1195,
        N1196,
        N1197,
        N1198,
        N1199,
        N1200,
        N1201,
        N1202,
        N1203,
        N1204,
        N1205,
        N1206,
        N1207,
        N1208,
        N1209,
        N1210,
        N1211,
        N1212,
        N1213,
        N1214,
        N1215,
        N1216,
        N1217,
        N1218,
        N1219,
        N1220,
        N1221,
        N1222,
        N1223,
        N1224,
        N1225,
        N1226,
        N1227,
        N1228,
        N1229,
        N1230,
        N1231,
        N1232,
        N1233,
        N1234,
        N1235,
        N1236,
        N1237,
        N1238,
        N1239,
        N1240,
        N1241,
        N1242,
        N1243,
        N1244,
        N1245,
        N1246,
        N1247,
        N1248,
        N1249,
        N1250,
        N1251,
        N1252,
        N1253,
        N1254,
        N1255,
        N1256,
        N1257,
        N1258,
        N1259,
        N1260,
        N1261,
        N1262,
        N1263,
        N1264,
        N1265,
        N1266,
        N1267,
        N1268,
        N1269,
        N1270,
        N1271,
        N1272,
        N1273,
        N1274,
        N1275,
        N1276,
        N1277,
        N1278,
        N1279,
        N1280,
        N1281,
        N1282,
        N1283,
        N1284,
        N1285,
        N1286,
        N1287,
        N1288,
        N1289,
        N1290,
        N1291,
        N1292,
        N1293,
        N1294,
        N1295,
        N1296,
        N1297,
        N1298,
        N1299,
        N1300,
        N1301,
        N1302,
        N1303,
        N1304,
        N1305,
        N1306,
        N1307,
        N1308,
        N1309,
        N1310,
        N1311,
        N1312,
        N1313,
        N1314,
        N1315,
        N1316,
        N1317,
        N1318,
        N1319,
        N1320,
        N1321,
        N1322,
        N1323,
        N1324,
        N1325,
        N1326,
        N1327,
        N1328,
        N1329,
        N1330,
        N1331,
        N1332,
        N1333,
        N1334,
        N1335,
        N1336,
        N1337,
        N1338,
        N1339,
        N1340,
        N1341,
        N1342,
        N1343,
        N1344,
        N1345,
        N1346,
        N1347,
        N1348,
        N1349,
        N1350,
        N1351,
        N1352,
        N1353,
        N1354,
        N1355,
        N1356,
        N1357,
        N1358,
        N1359,
        N1360,
        N1361,
        N1362,
        N1363,
        N1364,
        N1365,
        N1366,
        N1367,
        N1368,
        N1369,
        N1370,
        N1371,
        N1372,
        N1373,
        N1374,
        N1375,
        N1376,
        N1377,
        N1378,
        N1379,
        N1380,
        N1381,
        N1382,
        N1383,
        N1384,
        N1385,
        N1386,
        N1387,
        N1388,
        N1389,
        N1390,
        N1391,
        N1392,
        N1393,
        N1394,
        N1395,
        N1396,
        N1397,
        N1398,
        N1399,
        N1400,
        N1401,
        N1402,
        N1403,
        N1404,
        N1405,
        N1406,
        N1407,
        N1408,
        N1409,
        N1410,
        N1411,
        N1412,
        N1413,
        N1414,
        N1415,
        N1416,
        N1417,
        N1418,
        N1419,
        N1420,
        N1421,
        N1422,
        N1423,
        N1424,
        N1425,
        N1426,
        N1427,
        N1428,
        N1429,
        N1430,
        N1431,
        N1432,
        N1433,
        N1434,
        N1435,
        N1436,
        N1437,
        N1438,
        N1439,
        N1440,
        N1441,
        N1442,
        N1443,
        N1444,
        N1445,
        N1446,
        N1447,
        N1448,
        N1449,
        N1450,
        N1451,
        N1452,
        N1453,
        N1454,
        N1455,
        N1456,
        N1457,
        N1458,
        N1459,
        N1460,
        N1461,
        N1462,
        N1463,
        N1464,
        N1465,
        N1466,
        N1467,
        N1468,
        N1469,
        N1470,
        N1471,
        N1472,
        N1473,
        N1474,
        N1475,
        N1476,
        N1477,
        N1478,
        N1479,
        N1480,
        N1481,
        N1482,
        N1483,
        N1484,
        N1485,
        N1486,
        N1487,
        N1488,
        N1489,
        N1490,
        N1491,
        N1492,
        N1493,
        N1494,
        N1495,
        N1496,
        N1497,
        N1498,
        N1499,
        N1500,
        N1501,
        N1502,
        N1503,
        N1504,
        N1505,
        N1506,
        N1507,
        N1508,
        N1509,
        N1510,
        N1511,
        N1512,
        N1513,
        N1514,
        N1515,
        N1516,
        N1517,
        N1518,
        N1519,
        N1520,
        N1521,
        N1522,
        N1523,
        N1524,
        N1525,
        N1526,
        N1527,
        N1528,
        N1529,
        N1530,
        N1531,
        N1532,
        N1533,
        N1534,
        N1535,
        N1536,
        N1537,
        N1538,
        N1539,
        N1540,
        N1541,
        N1542,
        N1543,
        N1544,
        N1545,
        N1546,
        N1547,
        N1548,
        N1549,
        N1550,
        N1551,
        N1552,
        N1553,
        N1554,
        N1555,
        N1556,
        N1557,
        N1558,
        N1559,
        N1560,
        N1561,
        N1562,
        N1563,
        N1564,
        N1565,
        N1566,
        N1567,
        N1568,
        N1569,
        N1570,
        N1571,
        N1572,
        N1573,
        N1574,
        N1575,
        N1576,
        N1577,
        N1578,
        N1579,
        N1580,
        N1581,
        N1582,
        N1583,
        N1584,
        N1585,
        N1586,
        N1587,
        N1588,
        N1589,
        N1590,
        N1591,
        N1592,
        N1593,
        N1594,
        N1595,
        N1596,
        N1597,
        N1598,
        N1599,
    ];

    [MethodImpl(MethodImplOptions.NoInlining)] private static int N0(int x) => (x * 3) + 0;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1(int x) => (x * 4) + 1;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N2(int x) => (x * 5) + 2;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N3(int x) => (x * 6) + 3;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N4(int x) => (x * 7) + 4;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N5(int x) => (x * 8) + 5;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N6(int x) => (x * 9) + 6;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N7(int x) => (x * 10) + 7;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N8(int x) => (x * 11) + 8;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N9(int x) => (x * 12) + 9;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N10(int x) => (x * 13) + 10;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N11(int x) => (x * 14) + 11;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N12(int x) => (x * 15) + 12;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N13(int x) => (x * 16) + 13;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N14(int x) => (x * 17) + 14;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N15(int x) => (x * 18) + 15;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N16(int x) => (x * 19) + 16;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N17(int x) => (x * 20) + 17;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N18(int x) => (x * 21) + 18;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N19(int x) => (x * 22) + 19;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N20(int x) => (x * 23) + 20;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N21(int x) => (x * 24) + 21;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N22(int x) => (x * 25) + 22;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N23(int x) => (x * 26) + 23;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N24(int x) => (x * 27) + 24;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N25(int x) => (x * 28) + 25;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N26(int x) => (x * 29) + 26;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N27(int x) => (x * 30) + 27;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N28(int x) => (x * 31) + 28;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N29(int x) => (x * 32) + 29;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N30(int x) => (x * 33) + 30;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N31(int x) => (x * 34) + 31;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N32(int x) => (x * 35) + 32;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N33(int x) => (x * 36) + 33;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N34(int x) => (x * 37) + 34;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N35(int x) => (x * 38) + 35;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N36(int x) => (x * 39) + 36;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N37(int x) => (x * 40) + 37;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N38(int x) => (x * 41) + 38;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N39(int x) => (x * 42) + 39;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N40(int x) => (x * 43) + 40;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N41(int x) => (x * 44) + 41;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N42(int x) => (x * 45) + 42;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N43(int x) => (x * 46) + 43;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N44(int x) => (x * 47) + 44;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N45(int x) => (x * 48) + 45;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N46(int x) => (x * 49) + 46;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N47(int x) => (x * 50) + 47;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N48(int x) => (x * 51) + 48;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N49(int x) => (x * 52) + 49;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N50(int x) => (x * 53) + 50;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N51(int x) => (x * 54) + 51;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N52(int x) => (x * 55) + 52;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N53(int x) => (x * 56) + 53;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N54(int x) => (x * 57) + 54;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N55(int x) => (x * 58) + 55;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N56(int x) => (x * 59) + 56;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N57(int x) => (x * 60) + 57;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N58(int x) => (x * 61) + 58;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N59(int x) => (x * 62) + 59;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N60(int x) => (x * 63) + 60;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N61(int x) => (x * 64) + 61;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N62(int x) => (x * 65) + 62;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N63(int x) => (x * 66) + 63;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N64(int x) => (x * 67) + 64;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N65(int x) => (x * 68) + 65;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N66(int x) => (x * 69) + 66;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N67(int x) => (x * 70) + 67;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N68(int x) => (x * 71) + 68;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N69(int x) => (x * 72) + 69;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N70(int x) => (x * 73) + 70;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N71(int x) => (x * 74) + 71;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N72(int x) => (x * 75) + 72;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N73(int x) => (x * 76) + 73;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N74(int x) => (x * 77) + 74;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N75(int x) => (x * 78) + 75;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N76(int x) => (x * 79) + 76;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N77(int x) => (x * 80) + 77;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N78(int x) => (x * 81) + 78;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N79(int x) => (x * 82) + 79;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N80(int x) => (x * 83) + 80;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N81(int x) => (x * 84) + 81;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N82(int x) => (x * 85) + 82;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N83(int x) => (x * 86) + 83;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N84(int x) => (x * 87) + 84;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N85(int x) => (x * 88) + 85;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N86(int x) => (x * 89) + 86;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N87(int x) => (x * 90) + 87;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N88(int x) => (x * 91) + 88;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N89(int x) => (x * 92) + 89;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N90(int x) => (x * 93) + 90;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N91(int x) => (x * 94) + 91;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N92(int x) => (x * 95) + 92;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N93(int x) => (x * 96) + 93;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N94(int x) => (x * 97) + 94;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N95(int x) => (x * 98) + 95;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N96(int x) => (x * 99) + 96;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N97(int x) => (x * 100) + 97;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N98(int x) => (x * 101) + 98;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N99(int x) => (x * 102) + 99;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N100(int x) => (x * 103) + 100;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N101(int x) => (x * 104) + 101;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N102(int x) => (x * 105) + 102;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N103(int x) => (x * 106) + 103;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N104(int x) => (x * 107) + 104;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N105(int x) => (x * 108) + 105;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N106(int x) => (x * 109) + 106;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N107(int x) => (x * 110) + 107;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N108(int x) => (x * 111) + 108;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N109(int x) => (x * 112) + 109;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N110(int x) => (x * 113) + 110;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N111(int x) => (x * 114) + 111;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N112(int x) => (x * 115) + 112;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N113(int x) => (x * 116) + 113;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N114(int x) => (x * 117) + 114;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N115(int x) => (x * 118) + 115;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N116(int x) => (x * 119) + 116;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N117(int x) => (x * 120) + 117;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N118(int x) => (x * 121) + 118;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N119(int x) => (x * 122) + 119;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N120(int x) => (x * 123) + 120;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N121(int x) => (x * 124) + 121;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N122(int x) => (x * 125) + 122;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N123(int x) => (x * 126) + 123;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N124(int x) => (x * 127) + 124;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N125(int x) => (x * 128) + 125;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N126(int x) => (x * 129) + 126;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N127(int x) => (x * 130) + 127;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N128(int x) => (x * 131) + 128;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N129(int x) => (x * 132) + 129;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N130(int x) => (x * 133) + 130;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N131(int x) => (x * 134) + 131;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N132(int x) => (x * 135) + 132;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N133(int x) => (x * 136) + 133;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N134(int x) => (x * 137) + 134;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N135(int x) => (x * 138) + 135;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N136(int x) => (x * 139) + 136;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N137(int x) => (x * 140) + 137;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N138(int x) => (x * 141) + 138;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N139(int x) => (x * 142) + 139;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N140(int x) => (x * 143) + 140;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N141(int x) => (x * 144) + 141;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N142(int x) => (x * 145) + 142;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N143(int x) => (x * 146) + 143;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N144(int x) => (x * 147) + 144;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N145(int x) => (x * 148) + 145;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N146(int x) => (x * 149) + 146;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N147(int x) => (x * 150) + 147;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N148(int x) => (x * 151) + 148;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N149(int x) => (x * 152) + 149;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N150(int x) => (x * 153) + 150;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N151(int x) => (x * 154) + 151;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N152(int x) => (x * 155) + 152;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N153(int x) => (x * 156) + 153;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N154(int x) => (x * 157) + 154;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N155(int x) => (x * 158) + 155;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N156(int x) => (x * 159) + 156;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N157(int x) => (x * 160) + 157;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N158(int x) => (x * 161) + 158;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N159(int x) => (x * 162) + 159;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N160(int x) => (x * 163) + 160;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N161(int x) => (x * 164) + 161;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N162(int x) => (x * 165) + 162;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N163(int x) => (x * 166) + 163;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N164(int x) => (x * 167) + 164;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N165(int x) => (x * 168) + 165;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N166(int x) => (x * 169) + 166;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N167(int x) => (x * 170) + 167;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N168(int x) => (x * 171) + 168;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N169(int x) => (x * 172) + 169;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N170(int x) => (x * 173) + 170;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N171(int x) => (x * 174) + 171;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N172(int x) => (x * 175) + 172;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N173(int x) => (x * 176) + 173;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N174(int x) => (x * 177) + 174;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N175(int x) => (x * 178) + 175;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N176(int x) => (x * 179) + 176;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N177(int x) => (x * 180) + 177;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N178(int x) => (x * 181) + 178;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N179(int x) => (x * 182) + 179;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N180(int x) => (x * 183) + 180;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N181(int x) => (x * 184) + 181;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N182(int x) => (x * 185) + 182;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N183(int x) => (x * 186) + 183;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N184(int x) => (x * 187) + 184;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N185(int x) => (x * 188) + 185;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N186(int x) => (x * 189) + 186;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N187(int x) => (x * 190) + 187;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N188(int x) => (x * 191) + 188;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N189(int x) => (x * 192) + 189;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N190(int x) => (x * 193) + 190;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N191(int x) => (x * 194) + 191;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N192(int x) => (x * 195) + 192;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N193(int x) => (x * 196) + 193;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N194(int x) => (x * 197) + 194;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N195(int x) => (x * 198) + 195;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N196(int x) => (x * 199) + 196;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N197(int x) => (x * 200) + 197;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N198(int x) => (x * 201) + 198;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N199(int x) => (x * 202) + 199;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N200(int x) => (x * 203) + 200;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N201(int x) => (x * 204) + 201;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N202(int x) => (x * 205) + 202;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N203(int x) => (x * 206) + 203;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N204(int x) => (x * 207) + 204;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N205(int x) => (x * 208) + 205;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N206(int x) => (x * 209) + 206;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N207(int x) => (x * 210) + 207;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N208(int x) => (x * 211) + 208;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N209(int x) => (x * 212) + 209;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N210(int x) => (x * 213) + 210;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N211(int x) => (x * 214) + 211;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N212(int x) => (x * 215) + 212;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N213(int x) => (x * 216) + 213;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N214(int x) => (x * 217) + 214;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N215(int x) => (x * 218) + 215;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N216(int x) => (x * 219) + 216;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N217(int x) => (x * 220) + 217;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N218(int x) => (x * 221) + 218;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N219(int x) => (x * 222) + 219;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N220(int x) => (x * 223) + 220;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N221(int x) => (x * 224) + 221;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N222(int x) => (x * 225) + 222;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N223(int x) => (x * 226) + 223;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N224(int x) => (x * 227) + 224;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N225(int x) => (x * 228) + 225;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N226(int x) => (x * 229) + 226;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N227(int x) => (x * 230) + 227;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N228(int x) => (x * 231) + 228;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N229(int x) => (x * 232) + 229;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N230(int x) => (x * 233) + 230;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N231(int x) => (x * 234) + 231;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N232(int x) => (x * 235) + 232;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N233(int x) => (x * 236) + 233;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N234(int x) => (x * 237) + 234;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N235(int x) => (x * 238) + 235;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N236(int x) => (x * 239) + 236;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N237(int x) => (x * 240) + 237;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N238(int x) => (x * 241) + 238;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N239(int x) => (x * 242) + 239;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N240(int x) => (x * 243) + 240;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N241(int x) => (x * 244) + 241;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N242(int x) => (x * 245) + 242;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N243(int x) => (x * 246) + 243;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N244(int x) => (x * 247) + 244;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N245(int x) => (x * 248) + 245;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N246(int x) => (x * 249) + 246;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N247(int x) => (x * 250) + 247;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N248(int x) => (x * 251) + 248;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N249(int x) => (x * 252) + 249;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N250(int x) => (x * 253) + 250;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N251(int x) => (x * 254) + 251;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N252(int x) => (x * 255) + 252;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N253(int x) => (x * 256) + 253;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N254(int x) => (x * 257) + 254;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N255(int x) => (x * 258) + 255;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N256(int x) => (x * 259) + 256;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N257(int x) => (x * 260) + 257;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N258(int x) => (x * 261) + 258;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N259(int x) => (x * 262) + 259;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N260(int x) => (x * 263) + 260;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N261(int x) => (x * 264) + 261;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N262(int x) => (x * 265) + 262;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N263(int x) => (x * 266) + 263;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N264(int x) => (x * 267) + 264;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N265(int x) => (x * 268) + 265;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N266(int x) => (x * 269) + 266;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N267(int x) => (x * 270) + 267;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N268(int x) => (x * 271) + 268;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N269(int x) => (x * 272) + 269;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N270(int x) => (x * 273) + 270;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N271(int x) => (x * 274) + 271;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N272(int x) => (x * 275) + 272;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N273(int x) => (x * 276) + 273;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N274(int x) => (x * 277) + 274;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N275(int x) => (x * 278) + 275;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N276(int x) => (x * 279) + 276;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N277(int x) => (x * 280) + 277;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N278(int x) => (x * 281) + 278;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N279(int x) => (x * 282) + 279;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N280(int x) => (x * 283) + 280;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N281(int x) => (x * 284) + 281;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N282(int x) => (x * 285) + 282;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N283(int x) => (x * 286) + 283;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N284(int x) => (x * 287) + 284;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N285(int x) => (x * 288) + 285;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N286(int x) => (x * 289) + 286;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N287(int x) => (x * 290) + 287;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N288(int x) => (x * 291) + 288;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N289(int x) => (x * 292) + 289;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N290(int x) => (x * 293) + 290;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N291(int x) => (x * 294) + 291;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N292(int x) => (x * 295) + 292;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N293(int x) => (x * 296) + 293;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N294(int x) => (x * 297) + 294;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N295(int x) => (x * 298) + 295;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N296(int x) => (x * 299) + 296;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N297(int x) => (x * 300) + 297;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N298(int x) => (x * 301) + 298;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N299(int x) => (x * 302) + 299;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N300(int x) => (x * 303) + 300;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N301(int x) => (x * 304) + 301;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N302(int x) => (x * 305) + 302;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N303(int x) => (x * 306) + 303;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N304(int x) => (x * 307) + 304;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N305(int x) => (x * 308) + 305;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N306(int x) => (x * 309) + 306;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N307(int x) => (x * 310) + 307;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N308(int x) => (x * 311) + 308;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N309(int x) => (x * 312) + 309;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N310(int x) => (x * 313) + 310;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N311(int x) => (x * 314) + 311;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N312(int x) => (x * 315) + 312;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N313(int x) => (x * 316) + 313;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N314(int x) => (x * 317) + 314;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N315(int x) => (x * 318) + 315;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N316(int x) => (x * 319) + 316;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N317(int x) => (x * 320) + 317;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N318(int x) => (x * 321) + 318;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N319(int x) => (x * 322) + 319;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N320(int x) => (x * 323) + 320;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N321(int x) => (x * 324) + 321;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N322(int x) => (x * 325) + 322;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N323(int x) => (x * 326) + 323;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N324(int x) => (x * 327) + 324;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N325(int x) => (x * 328) + 325;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N326(int x) => (x * 329) + 326;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N327(int x) => (x * 330) + 327;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N328(int x) => (x * 331) + 328;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N329(int x) => (x * 332) + 329;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N330(int x) => (x * 333) + 330;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N331(int x) => (x * 334) + 331;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N332(int x) => (x * 335) + 332;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N333(int x) => (x * 336) + 333;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N334(int x) => (x * 337) + 334;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N335(int x) => (x * 338) + 335;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N336(int x) => (x * 339) + 336;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N337(int x) => (x * 340) + 337;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N338(int x) => (x * 341) + 338;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N339(int x) => (x * 342) + 339;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N340(int x) => (x * 343) + 340;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N341(int x) => (x * 344) + 341;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N342(int x) => (x * 345) + 342;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N343(int x) => (x * 346) + 343;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N344(int x) => (x * 347) + 344;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N345(int x) => (x * 348) + 345;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N346(int x) => (x * 349) + 346;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N347(int x) => (x * 350) + 347;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N348(int x) => (x * 351) + 348;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N349(int x) => (x * 352) + 349;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N350(int x) => (x * 353) + 350;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N351(int x) => (x * 354) + 351;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N352(int x) => (x * 355) + 352;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N353(int x) => (x * 356) + 353;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N354(int x) => (x * 357) + 354;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N355(int x) => (x * 358) + 355;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N356(int x) => (x * 359) + 356;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N357(int x) => (x * 360) + 357;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N358(int x) => (x * 361) + 358;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N359(int x) => (x * 362) + 359;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N360(int x) => (x * 363) + 360;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N361(int x) => (x * 364) + 361;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N362(int x) => (x * 365) + 362;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N363(int x) => (x * 366) + 363;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N364(int x) => (x * 367) + 364;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N365(int x) => (x * 368) + 365;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N366(int x) => (x * 369) + 366;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N367(int x) => (x * 370) + 367;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N368(int x) => (x * 371) + 368;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N369(int x) => (x * 372) + 369;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N370(int x) => (x * 373) + 370;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N371(int x) => (x * 374) + 371;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N372(int x) => (x * 375) + 372;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N373(int x) => (x * 376) + 373;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N374(int x) => (x * 377) + 374;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N375(int x) => (x * 378) + 375;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N376(int x) => (x * 379) + 376;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N377(int x) => (x * 380) + 377;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N378(int x) => (x * 381) + 378;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N379(int x) => (x * 382) + 379;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N380(int x) => (x * 383) + 380;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N381(int x) => (x * 384) + 381;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N382(int x) => (x * 385) + 382;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N383(int x) => (x * 386) + 383;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N384(int x) => (x * 387) + 384;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N385(int x) => (x * 388) + 385;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N386(int x) => (x * 389) + 386;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N387(int x) => (x * 390) + 387;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N388(int x) => (x * 391) + 388;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N389(int x) => (x * 392) + 389;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N390(int x) => (x * 393) + 390;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N391(int x) => (x * 394) + 391;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N392(int x) => (x * 395) + 392;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N393(int x) => (x * 396) + 393;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N394(int x) => (x * 397) + 394;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N395(int x) => (x * 398) + 395;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N396(int x) => (x * 399) + 396;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N397(int x) => (x * 400) + 397;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N398(int x) => (x * 401) + 398;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N399(int x) => (x * 402) + 399;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N400(int x) => (x * 403) + 400;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N401(int x) => (x * 404) + 401;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N402(int x) => (x * 405) + 402;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N403(int x) => (x * 406) + 403;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N404(int x) => (x * 407) + 404;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N405(int x) => (x * 408) + 405;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N406(int x) => (x * 409) + 406;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N407(int x) => (x * 410) + 407;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N408(int x) => (x * 411) + 408;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N409(int x) => (x * 412) + 409;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N410(int x) => (x * 413) + 410;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N411(int x) => (x * 414) + 411;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N412(int x) => (x * 415) + 412;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N413(int x) => (x * 416) + 413;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N414(int x) => (x * 417) + 414;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N415(int x) => (x * 418) + 415;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N416(int x) => (x * 419) + 416;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N417(int x) => (x * 420) + 417;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N418(int x) => (x * 421) + 418;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N419(int x) => (x * 422) + 419;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N420(int x) => (x * 423) + 420;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N421(int x) => (x * 424) + 421;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N422(int x) => (x * 425) + 422;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N423(int x) => (x * 426) + 423;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N424(int x) => (x * 427) + 424;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N425(int x) => (x * 428) + 425;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N426(int x) => (x * 429) + 426;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N427(int x) => (x * 430) + 427;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N428(int x) => (x * 431) + 428;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N429(int x) => (x * 432) + 429;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N430(int x) => (x * 433) + 430;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N431(int x) => (x * 434) + 431;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N432(int x) => (x * 435) + 432;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N433(int x) => (x * 436) + 433;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N434(int x) => (x * 437) + 434;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N435(int x) => (x * 438) + 435;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N436(int x) => (x * 439) + 436;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N437(int x) => (x * 440) + 437;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N438(int x) => (x * 441) + 438;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N439(int x) => (x * 442) + 439;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N440(int x) => (x * 443) + 440;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N441(int x) => (x * 444) + 441;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N442(int x) => (x * 445) + 442;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N443(int x) => (x * 446) + 443;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N444(int x) => (x * 447) + 444;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N445(int x) => (x * 448) + 445;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N446(int x) => (x * 449) + 446;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N447(int x) => (x * 450) + 447;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N448(int x) => (x * 451) + 448;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N449(int x) => (x * 452) + 449;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N450(int x) => (x * 453) + 450;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N451(int x) => (x * 454) + 451;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N452(int x) => (x * 455) + 452;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N453(int x) => (x * 456) + 453;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N454(int x) => (x * 457) + 454;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N455(int x) => (x * 458) + 455;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N456(int x) => (x * 459) + 456;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N457(int x) => (x * 460) + 457;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N458(int x) => (x * 461) + 458;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N459(int x) => (x * 462) + 459;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N460(int x) => (x * 463) + 460;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N461(int x) => (x * 464) + 461;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N462(int x) => (x * 465) + 462;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N463(int x) => (x * 466) + 463;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N464(int x) => (x * 467) + 464;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N465(int x) => (x * 468) + 465;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N466(int x) => (x * 469) + 466;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N467(int x) => (x * 470) + 467;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N468(int x) => (x * 471) + 468;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N469(int x) => (x * 472) + 469;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N470(int x) => (x * 473) + 470;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N471(int x) => (x * 474) + 471;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N472(int x) => (x * 475) + 472;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N473(int x) => (x * 476) + 473;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N474(int x) => (x * 477) + 474;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N475(int x) => (x * 478) + 475;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N476(int x) => (x * 479) + 476;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N477(int x) => (x * 480) + 477;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N478(int x) => (x * 481) + 478;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N479(int x) => (x * 482) + 479;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N480(int x) => (x * 483) + 480;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N481(int x) => (x * 484) + 481;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N482(int x) => (x * 485) + 482;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N483(int x) => (x * 486) + 483;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N484(int x) => (x * 487) + 484;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N485(int x) => (x * 488) + 485;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N486(int x) => (x * 489) + 486;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N487(int x) => (x * 490) + 487;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N488(int x) => (x * 491) + 488;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N489(int x) => (x * 492) + 489;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N490(int x) => (x * 493) + 490;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N491(int x) => (x * 494) + 491;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N492(int x) => (x * 495) + 492;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N493(int x) => (x * 496) + 493;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N494(int x) => (x * 497) + 494;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N495(int x) => (x * 498) + 495;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N496(int x) => (x * 499) + 496;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N497(int x) => (x * 500) + 497;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N498(int x) => (x * 501) + 498;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N499(int x) => (x * 502) + 499;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N500(int x) => (x * 503) + 500;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N501(int x) => (x * 504) + 501;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N502(int x) => (x * 505) + 502;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N503(int x) => (x * 506) + 503;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N504(int x) => (x * 507) + 504;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N505(int x) => (x * 508) + 505;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N506(int x) => (x * 509) + 506;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N507(int x) => (x * 510) + 507;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N508(int x) => (x * 511) + 508;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N509(int x) => (x * 512) + 509;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N510(int x) => (x * 513) + 510;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N511(int x) => (x * 514) + 511;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N512(int x) => (x * 515) + 512;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N513(int x) => (x * 516) + 513;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N514(int x) => (x * 517) + 514;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N515(int x) => (x * 518) + 515;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N516(int x) => (x * 519) + 516;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N517(int x) => (x * 520) + 517;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N518(int x) => (x * 521) + 518;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N519(int x) => (x * 522) + 519;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N520(int x) => (x * 523) + 520;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N521(int x) => (x * 524) + 521;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N522(int x) => (x * 525) + 522;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N523(int x) => (x * 526) + 523;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N524(int x) => (x * 527) + 524;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N525(int x) => (x * 528) + 525;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N526(int x) => (x * 529) + 526;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N527(int x) => (x * 530) + 527;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N528(int x) => (x * 531) + 528;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N529(int x) => (x * 532) + 529;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N530(int x) => (x * 533) + 530;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N531(int x) => (x * 534) + 531;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N532(int x) => (x * 535) + 532;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N533(int x) => (x * 536) + 533;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N534(int x) => (x * 537) + 534;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N535(int x) => (x * 538) + 535;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N536(int x) => (x * 539) + 536;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N537(int x) => (x * 540) + 537;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N538(int x) => (x * 541) + 538;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N539(int x) => (x * 542) + 539;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N540(int x) => (x * 543) + 540;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N541(int x) => (x * 544) + 541;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N542(int x) => (x * 545) + 542;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N543(int x) => (x * 546) + 543;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N544(int x) => (x * 547) + 544;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N545(int x) => (x * 548) + 545;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N546(int x) => (x * 549) + 546;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N547(int x) => (x * 550) + 547;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N548(int x) => (x * 551) + 548;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N549(int x) => (x * 552) + 549;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N550(int x) => (x * 553) + 550;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N551(int x) => (x * 554) + 551;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N552(int x) => (x * 555) + 552;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N553(int x) => (x * 556) + 553;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N554(int x) => (x * 557) + 554;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N555(int x) => (x * 558) + 555;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N556(int x) => (x * 559) + 556;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N557(int x) => (x * 560) + 557;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N558(int x) => (x * 561) + 558;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N559(int x) => (x * 562) + 559;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N560(int x) => (x * 563) + 560;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N561(int x) => (x * 564) + 561;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N562(int x) => (x * 565) + 562;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N563(int x) => (x * 566) + 563;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N564(int x) => (x * 567) + 564;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N565(int x) => (x * 568) + 565;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N566(int x) => (x * 569) + 566;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N567(int x) => (x * 570) + 567;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N568(int x) => (x * 571) + 568;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N569(int x) => (x * 572) + 569;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N570(int x) => (x * 573) + 570;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N571(int x) => (x * 574) + 571;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N572(int x) => (x * 575) + 572;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N573(int x) => (x * 576) + 573;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N574(int x) => (x * 577) + 574;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N575(int x) => (x * 578) + 575;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N576(int x) => (x * 579) + 576;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N577(int x) => (x * 580) + 577;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N578(int x) => (x * 581) + 578;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N579(int x) => (x * 582) + 579;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N580(int x) => (x * 583) + 580;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N581(int x) => (x * 584) + 581;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N582(int x) => (x * 585) + 582;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N583(int x) => (x * 586) + 583;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N584(int x) => (x * 587) + 584;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N585(int x) => (x * 588) + 585;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N586(int x) => (x * 589) + 586;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N587(int x) => (x * 590) + 587;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N588(int x) => (x * 591) + 588;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N589(int x) => (x * 592) + 589;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N590(int x) => (x * 593) + 590;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N591(int x) => (x * 594) + 591;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N592(int x) => (x * 595) + 592;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N593(int x) => (x * 596) + 593;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N594(int x) => (x * 597) + 594;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N595(int x) => (x * 598) + 595;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N596(int x) => (x * 599) + 596;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N597(int x) => (x * 600) + 597;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N598(int x) => (x * 601) + 598;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N599(int x) => (x * 602) + 599;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N600(int x) => (x * 603) + 600;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N601(int x) => (x * 604) + 601;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N602(int x) => (x * 605) + 602;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N603(int x) => (x * 606) + 603;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N604(int x) => (x * 607) + 604;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N605(int x) => (x * 608) + 605;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N606(int x) => (x * 609) + 606;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N607(int x) => (x * 610) + 607;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N608(int x) => (x * 611) + 608;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N609(int x) => (x * 612) + 609;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N610(int x) => (x * 613) + 610;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N611(int x) => (x * 614) + 611;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N612(int x) => (x * 615) + 612;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N613(int x) => (x * 616) + 613;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N614(int x) => (x * 617) + 614;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N615(int x) => (x * 618) + 615;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N616(int x) => (x * 619) + 616;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N617(int x) => (x * 620) + 617;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N618(int x) => (x * 621) + 618;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N619(int x) => (x * 622) + 619;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N620(int x) => (x * 623) + 620;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N621(int x) => (x * 624) + 621;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N622(int x) => (x * 625) + 622;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N623(int x) => (x * 626) + 623;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N624(int x) => (x * 627) + 624;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N625(int x) => (x * 628) + 625;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N626(int x) => (x * 629) + 626;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N627(int x) => (x * 630) + 627;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N628(int x) => (x * 631) + 628;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N629(int x) => (x * 632) + 629;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N630(int x) => (x * 633) + 630;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N631(int x) => (x * 634) + 631;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N632(int x) => (x * 635) + 632;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N633(int x) => (x * 636) + 633;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N634(int x) => (x * 637) + 634;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N635(int x) => (x * 638) + 635;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N636(int x) => (x * 639) + 636;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N637(int x) => (x * 640) + 637;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N638(int x) => (x * 641) + 638;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N639(int x) => (x * 642) + 639;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N640(int x) => (x * 643) + 640;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N641(int x) => (x * 644) + 641;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N642(int x) => (x * 645) + 642;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N643(int x) => (x * 646) + 643;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N644(int x) => (x * 647) + 644;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N645(int x) => (x * 648) + 645;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N646(int x) => (x * 649) + 646;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N647(int x) => (x * 650) + 647;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N648(int x) => (x * 651) + 648;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N649(int x) => (x * 652) + 649;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N650(int x) => (x * 653) + 650;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N651(int x) => (x * 654) + 651;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N652(int x) => (x * 655) + 652;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N653(int x) => (x * 656) + 653;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N654(int x) => (x * 657) + 654;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N655(int x) => (x * 658) + 655;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N656(int x) => (x * 659) + 656;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N657(int x) => (x * 660) + 657;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N658(int x) => (x * 661) + 658;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N659(int x) => (x * 662) + 659;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N660(int x) => (x * 663) + 660;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N661(int x) => (x * 664) + 661;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N662(int x) => (x * 665) + 662;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N663(int x) => (x * 666) + 663;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N664(int x) => (x * 667) + 664;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N665(int x) => (x * 668) + 665;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N666(int x) => (x * 669) + 666;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N667(int x) => (x * 670) + 667;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N668(int x) => (x * 671) + 668;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N669(int x) => (x * 672) + 669;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N670(int x) => (x * 673) + 670;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N671(int x) => (x * 674) + 671;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N672(int x) => (x * 675) + 672;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N673(int x) => (x * 676) + 673;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N674(int x) => (x * 677) + 674;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N675(int x) => (x * 678) + 675;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N676(int x) => (x * 679) + 676;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N677(int x) => (x * 680) + 677;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N678(int x) => (x * 681) + 678;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N679(int x) => (x * 682) + 679;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N680(int x) => (x * 683) + 680;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N681(int x) => (x * 684) + 681;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N682(int x) => (x * 685) + 682;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N683(int x) => (x * 686) + 683;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N684(int x) => (x * 687) + 684;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N685(int x) => (x * 688) + 685;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N686(int x) => (x * 689) + 686;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N687(int x) => (x * 690) + 687;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N688(int x) => (x * 691) + 688;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N689(int x) => (x * 692) + 689;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N690(int x) => (x * 693) + 690;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N691(int x) => (x * 694) + 691;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N692(int x) => (x * 695) + 692;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N693(int x) => (x * 696) + 693;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N694(int x) => (x * 697) + 694;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N695(int x) => (x * 698) + 695;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N696(int x) => (x * 699) + 696;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N697(int x) => (x * 700) + 697;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N698(int x) => (x * 701) + 698;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N699(int x) => (x * 702) + 699;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N700(int x) => (x * 703) + 700;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N701(int x) => (x * 704) + 701;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N702(int x) => (x * 705) + 702;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N703(int x) => (x * 706) + 703;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N704(int x) => (x * 707) + 704;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N705(int x) => (x * 708) + 705;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N706(int x) => (x * 709) + 706;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N707(int x) => (x * 710) + 707;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N708(int x) => (x * 711) + 708;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N709(int x) => (x * 712) + 709;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N710(int x) => (x * 713) + 710;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N711(int x) => (x * 714) + 711;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N712(int x) => (x * 715) + 712;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N713(int x) => (x * 716) + 713;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N714(int x) => (x * 717) + 714;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N715(int x) => (x * 718) + 715;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N716(int x) => (x * 719) + 716;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N717(int x) => (x * 720) + 717;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N718(int x) => (x * 721) + 718;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N719(int x) => (x * 722) + 719;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N720(int x) => (x * 723) + 720;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N721(int x) => (x * 724) + 721;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N722(int x) => (x * 725) + 722;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N723(int x) => (x * 726) + 723;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N724(int x) => (x * 727) + 724;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N725(int x) => (x * 728) + 725;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N726(int x) => (x * 729) + 726;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N727(int x) => (x * 730) + 727;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N728(int x) => (x * 731) + 728;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N729(int x) => (x * 732) + 729;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N730(int x) => (x * 733) + 730;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N731(int x) => (x * 734) + 731;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N732(int x) => (x * 735) + 732;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N733(int x) => (x * 736) + 733;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N734(int x) => (x * 737) + 734;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N735(int x) => (x * 738) + 735;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N736(int x) => (x * 739) + 736;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N737(int x) => (x * 740) + 737;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N738(int x) => (x * 741) + 738;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N739(int x) => (x * 742) + 739;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N740(int x) => (x * 743) + 740;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N741(int x) => (x * 744) + 741;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N742(int x) => (x * 745) + 742;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N743(int x) => (x * 746) + 743;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N744(int x) => (x * 747) + 744;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N745(int x) => (x * 748) + 745;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N746(int x) => (x * 749) + 746;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N747(int x) => (x * 750) + 747;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N748(int x) => (x * 751) + 748;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N749(int x) => (x * 752) + 749;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N750(int x) => (x * 753) + 750;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N751(int x) => (x * 754) + 751;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N752(int x) => (x * 755) + 752;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N753(int x) => (x * 756) + 753;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N754(int x) => (x * 757) + 754;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N755(int x) => (x * 758) + 755;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N756(int x) => (x * 759) + 756;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N757(int x) => (x * 760) + 757;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N758(int x) => (x * 761) + 758;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N759(int x) => (x * 762) + 759;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N760(int x) => (x * 763) + 760;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N761(int x) => (x * 764) + 761;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N762(int x) => (x * 765) + 762;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N763(int x) => (x * 766) + 763;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N764(int x) => (x * 767) + 764;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N765(int x) => (x * 768) + 765;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N766(int x) => (x * 769) + 766;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N767(int x) => (x * 770) + 767;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N768(int x) => (x * 771) + 768;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N769(int x) => (x * 772) + 769;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N770(int x) => (x * 773) + 770;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N771(int x) => (x * 774) + 771;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N772(int x) => (x * 775) + 772;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N773(int x) => (x * 776) + 773;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N774(int x) => (x * 777) + 774;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N775(int x) => (x * 778) + 775;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N776(int x) => (x * 779) + 776;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N777(int x) => (x * 780) + 777;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N778(int x) => (x * 781) + 778;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N779(int x) => (x * 782) + 779;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N780(int x) => (x * 783) + 780;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N781(int x) => (x * 784) + 781;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N782(int x) => (x * 785) + 782;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N783(int x) => (x * 786) + 783;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N784(int x) => (x * 787) + 784;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N785(int x) => (x * 788) + 785;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N786(int x) => (x * 789) + 786;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N787(int x) => (x * 790) + 787;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N788(int x) => (x * 791) + 788;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N789(int x) => (x * 792) + 789;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N790(int x) => (x * 793) + 790;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N791(int x) => (x * 794) + 791;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N792(int x) => (x * 795) + 792;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N793(int x) => (x * 796) + 793;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N794(int x) => (x * 797) + 794;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N795(int x) => (x * 798) + 795;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N796(int x) => (x * 799) + 796;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N797(int x) => (x * 800) + 797;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N798(int x) => (x * 801) + 798;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N799(int x) => (x * 802) + 799;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N800(int x) => (x * 803) + 800;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N801(int x) => (x * 804) + 801;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N802(int x) => (x * 805) + 802;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N803(int x) => (x * 806) + 803;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N804(int x) => (x * 807) + 804;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N805(int x) => (x * 808) + 805;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N806(int x) => (x * 809) + 806;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N807(int x) => (x * 810) + 807;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N808(int x) => (x * 811) + 808;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N809(int x) => (x * 812) + 809;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N810(int x) => (x * 813) + 810;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N811(int x) => (x * 814) + 811;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N812(int x) => (x * 815) + 812;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N813(int x) => (x * 816) + 813;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N814(int x) => (x * 817) + 814;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N815(int x) => (x * 818) + 815;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N816(int x) => (x * 819) + 816;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N817(int x) => (x * 820) + 817;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N818(int x) => (x * 821) + 818;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N819(int x) => (x * 822) + 819;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N820(int x) => (x * 823) + 820;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N821(int x) => (x * 824) + 821;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N822(int x) => (x * 825) + 822;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N823(int x) => (x * 826) + 823;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N824(int x) => (x * 827) + 824;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N825(int x) => (x * 828) + 825;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N826(int x) => (x * 829) + 826;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N827(int x) => (x * 830) + 827;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N828(int x) => (x * 831) + 828;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N829(int x) => (x * 832) + 829;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N830(int x) => (x * 833) + 830;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N831(int x) => (x * 834) + 831;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N832(int x) => (x * 835) + 832;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N833(int x) => (x * 836) + 833;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N834(int x) => (x * 837) + 834;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N835(int x) => (x * 838) + 835;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N836(int x) => (x * 839) + 836;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N837(int x) => (x * 840) + 837;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N838(int x) => (x * 841) + 838;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N839(int x) => (x * 842) + 839;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N840(int x) => (x * 843) + 840;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N841(int x) => (x * 844) + 841;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N842(int x) => (x * 845) + 842;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N843(int x) => (x * 846) + 843;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N844(int x) => (x * 847) + 844;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N845(int x) => (x * 848) + 845;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N846(int x) => (x * 849) + 846;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N847(int x) => (x * 850) + 847;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N848(int x) => (x * 851) + 848;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N849(int x) => (x * 852) + 849;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N850(int x) => (x * 853) + 850;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N851(int x) => (x * 854) + 851;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N852(int x) => (x * 855) + 852;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N853(int x) => (x * 856) + 853;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N854(int x) => (x * 857) + 854;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N855(int x) => (x * 858) + 855;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N856(int x) => (x * 859) + 856;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N857(int x) => (x * 860) + 857;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N858(int x) => (x * 861) + 858;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N859(int x) => (x * 862) + 859;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N860(int x) => (x * 863) + 860;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N861(int x) => (x * 864) + 861;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N862(int x) => (x * 865) + 862;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N863(int x) => (x * 866) + 863;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N864(int x) => (x * 867) + 864;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N865(int x) => (x * 868) + 865;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N866(int x) => (x * 869) + 866;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N867(int x) => (x * 870) + 867;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N868(int x) => (x * 871) + 868;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N869(int x) => (x * 872) + 869;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N870(int x) => (x * 873) + 870;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N871(int x) => (x * 874) + 871;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N872(int x) => (x * 875) + 872;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N873(int x) => (x * 876) + 873;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N874(int x) => (x * 877) + 874;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N875(int x) => (x * 878) + 875;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N876(int x) => (x * 879) + 876;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N877(int x) => (x * 880) + 877;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N878(int x) => (x * 881) + 878;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N879(int x) => (x * 882) + 879;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N880(int x) => (x * 883) + 880;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N881(int x) => (x * 884) + 881;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N882(int x) => (x * 885) + 882;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N883(int x) => (x * 886) + 883;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N884(int x) => (x * 887) + 884;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N885(int x) => (x * 888) + 885;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N886(int x) => (x * 889) + 886;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N887(int x) => (x * 890) + 887;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N888(int x) => (x * 891) + 888;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N889(int x) => (x * 892) + 889;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N890(int x) => (x * 893) + 890;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N891(int x) => (x * 894) + 891;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N892(int x) => (x * 895) + 892;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N893(int x) => (x * 896) + 893;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N894(int x) => (x * 897) + 894;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N895(int x) => (x * 898) + 895;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N896(int x) => (x * 899) + 896;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N897(int x) => (x * 900) + 897;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N898(int x) => (x * 901) + 898;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N899(int x) => (x * 902) + 899;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N900(int x) => (x * 903) + 900;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N901(int x) => (x * 904) + 901;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N902(int x) => (x * 905) + 902;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N903(int x) => (x * 906) + 903;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N904(int x) => (x * 907) + 904;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N905(int x) => (x * 908) + 905;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N906(int x) => (x * 909) + 906;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N907(int x) => (x * 910) + 907;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N908(int x) => (x * 911) + 908;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N909(int x) => (x * 912) + 909;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N910(int x) => (x * 913) + 910;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N911(int x) => (x * 914) + 911;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N912(int x) => (x * 915) + 912;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N913(int x) => (x * 916) + 913;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N914(int x) => (x * 917) + 914;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N915(int x) => (x * 918) + 915;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N916(int x) => (x * 919) + 916;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N917(int x) => (x * 920) + 917;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N918(int x) => (x * 921) + 918;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N919(int x) => (x * 922) + 919;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N920(int x) => (x * 923) + 920;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N921(int x) => (x * 924) + 921;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N922(int x) => (x * 925) + 922;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N923(int x) => (x * 926) + 923;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N924(int x) => (x * 927) + 924;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N925(int x) => (x * 928) + 925;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N926(int x) => (x * 929) + 926;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N927(int x) => (x * 930) + 927;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N928(int x) => (x * 931) + 928;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N929(int x) => (x * 932) + 929;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N930(int x) => (x * 933) + 930;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N931(int x) => (x * 934) + 931;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N932(int x) => (x * 935) + 932;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N933(int x) => (x * 936) + 933;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N934(int x) => (x * 937) + 934;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N935(int x) => (x * 938) + 935;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N936(int x) => (x * 939) + 936;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N937(int x) => (x * 940) + 937;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N938(int x) => (x * 941) + 938;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N939(int x) => (x * 942) + 939;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N940(int x) => (x * 943) + 940;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N941(int x) => (x * 944) + 941;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N942(int x) => (x * 945) + 942;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N943(int x) => (x * 946) + 943;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N944(int x) => (x * 947) + 944;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N945(int x) => (x * 948) + 945;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N946(int x) => (x * 949) + 946;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N947(int x) => (x * 950) + 947;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N948(int x) => (x * 951) + 948;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N949(int x) => (x * 952) + 949;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N950(int x) => (x * 953) + 950;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N951(int x) => (x * 954) + 951;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N952(int x) => (x * 955) + 952;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N953(int x) => (x * 956) + 953;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N954(int x) => (x * 957) + 954;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N955(int x) => (x * 958) + 955;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N956(int x) => (x * 959) + 956;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N957(int x) => (x * 960) + 957;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N958(int x) => (x * 961) + 958;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N959(int x) => (x * 962) + 959;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N960(int x) => (x * 963) + 960;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N961(int x) => (x * 964) + 961;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N962(int x) => (x * 965) + 962;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N963(int x) => (x * 966) + 963;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N964(int x) => (x * 967) + 964;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N965(int x) => (x * 968) + 965;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N966(int x) => (x * 969) + 966;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N967(int x) => (x * 970) + 967;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N968(int x) => (x * 971) + 968;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N969(int x) => (x * 972) + 969;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N970(int x) => (x * 973) + 970;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N971(int x) => (x * 974) + 971;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N972(int x) => (x * 975) + 972;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N973(int x) => (x * 976) + 973;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N974(int x) => (x * 977) + 974;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N975(int x) => (x * 978) + 975;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N976(int x) => (x * 979) + 976;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N977(int x) => (x * 980) + 977;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N978(int x) => (x * 981) + 978;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N979(int x) => (x * 982) + 979;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N980(int x) => (x * 983) + 980;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N981(int x) => (x * 984) + 981;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N982(int x) => (x * 985) + 982;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N983(int x) => (x * 986) + 983;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N984(int x) => (x * 987) + 984;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N985(int x) => (x * 988) + 985;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N986(int x) => (x * 989) + 986;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N987(int x) => (x * 990) + 987;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N988(int x) => (x * 991) + 988;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N989(int x) => (x * 992) + 989;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N990(int x) => (x * 993) + 990;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N991(int x) => (x * 994) + 991;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N992(int x) => (x * 995) + 992;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N993(int x) => (x * 996) + 993;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N994(int x) => (x * 997) + 994;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N995(int x) => (x * 998) + 995;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N996(int x) => (x * 999) + 996;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N997(int x) => (x * 1000) + 997;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N998(int x) => (x * 1001) + 998;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N999(int x) => (x * 1002) + 999;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1000(int x) => (x * 1003) + 1000;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1001(int x) => (x * 1004) + 1001;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1002(int x) => (x * 1005) + 1002;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1003(int x) => (x * 1006) + 1003;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1004(int x) => (x * 1007) + 1004;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1005(int x) => (x * 1008) + 1005;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1006(int x) => (x * 1009) + 1006;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1007(int x) => (x * 1010) + 1007;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1008(int x) => (x * 1011) + 1008;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1009(int x) => (x * 1012) + 1009;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1010(int x) => (x * 1013) + 1010;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1011(int x) => (x * 1014) + 1011;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1012(int x) => (x * 1015) + 1012;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1013(int x) => (x * 1016) + 1013;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1014(int x) => (x * 1017) + 1014;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1015(int x) => (x * 1018) + 1015;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1016(int x) => (x * 1019) + 1016;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1017(int x) => (x * 1020) + 1017;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1018(int x) => (x * 1021) + 1018;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1019(int x) => (x * 1022) + 1019;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1020(int x) => (x * 1023) + 1020;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1021(int x) => (x * 1024) + 1021;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1022(int x) => (x * 1025) + 1022;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1023(int x) => (x * 1026) + 1023;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1024(int x) => (x * 1027) + 1024;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1025(int x) => (x * 1028) + 1025;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1026(int x) => (x * 1029) + 1026;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1027(int x) => (x * 1030) + 1027;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1028(int x) => (x * 1031) + 1028;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1029(int x) => (x * 1032) + 1029;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1030(int x) => (x * 1033) + 1030;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1031(int x) => (x * 1034) + 1031;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1032(int x) => (x * 1035) + 1032;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1033(int x) => (x * 1036) + 1033;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1034(int x) => (x * 1037) + 1034;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1035(int x) => (x * 1038) + 1035;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1036(int x) => (x * 1039) + 1036;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1037(int x) => (x * 1040) + 1037;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1038(int x) => (x * 1041) + 1038;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1039(int x) => (x * 1042) + 1039;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1040(int x) => (x * 1043) + 1040;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1041(int x) => (x * 1044) + 1041;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1042(int x) => (x * 1045) + 1042;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1043(int x) => (x * 1046) + 1043;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1044(int x) => (x * 1047) + 1044;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1045(int x) => (x * 1048) + 1045;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1046(int x) => (x * 1049) + 1046;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1047(int x) => (x * 1050) + 1047;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1048(int x) => (x * 1051) + 1048;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1049(int x) => (x * 1052) + 1049;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1050(int x) => (x * 1053) + 1050;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1051(int x) => (x * 1054) + 1051;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1052(int x) => (x * 1055) + 1052;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1053(int x) => (x * 1056) + 1053;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1054(int x) => (x * 1057) + 1054;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1055(int x) => (x * 1058) + 1055;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1056(int x) => (x * 1059) + 1056;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1057(int x) => (x * 1060) + 1057;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1058(int x) => (x * 1061) + 1058;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1059(int x) => (x * 1062) + 1059;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1060(int x) => (x * 1063) + 1060;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1061(int x) => (x * 1064) + 1061;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1062(int x) => (x * 1065) + 1062;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1063(int x) => (x * 1066) + 1063;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1064(int x) => (x * 1067) + 1064;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1065(int x) => (x * 1068) + 1065;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1066(int x) => (x * 1069) + 1066;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1067(int x) => (x * 1070) + 1067;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1068(int x) => (x * 1071) + 1068;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1069(int x) => (x * 1072) + 1069;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1070(int x) => (x * 1073) + 1070;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1071(int x) => (x * 1074) + 1071;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1072(int x) => (x * 1075) + 1072;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1073(int x) => (x * 1076) + 1073;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1074(int x) => (x * 1077) + 1074;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1075(int x) => (x * 1078) + 1075;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1076(int x) => (x * 1079) + 1076;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1077(int x) => (x * 1080) + 1077;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1078(int x) => (x * 1081) + 1078;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1079(int x) => (x * 1082) + 1079;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1080(int x) => (x * 1083) + 1080;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1081(int x) => (x * 1084) + 1081;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1082(int x) => (x * 1085) + 1082;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1083(int x) => (x * 1086) + 1083;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1084(int x) => (x * 1087) + 1084;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1085(int x) => (x * 1088) + 1085;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1086(int x) => (x * 1089) + 1086;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1087(int x) => (x * 1090) + 1087;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1088(int x) => (x * 1091) + 1088;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1089(int x) => (x * 1092) + 1089;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1090(int x) => (x * 1093) + 1090;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1091(int x) => (x * 1094) + 1091;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1092(int x) => (x * 1095) + 1092;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1093(int x) => (x * 1096) + 1093;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1094(int x) => (x * 1097) + 1094;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1095(int x) => (x * 1098) + 1095;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1096(int x) => (x * 1099) + 1096;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1097(int x) => (x * 1100) + 1097;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1098(int x) => (x * 1101) + 1098;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1099(int x) => (x * 1102) + 1099;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1100(int x) => (x * 1103) + 1100;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1101(int x) => (x * 1104) + 1101;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1102(int x) => (x * 1105) + 1102;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1103(int x) => (x * 1106) + 1103;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1104(int x) => (x * 1107) + 1104;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1105(int x) => (x * 1108) + 1105;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1106(int x) => (x * 1109) + 1106;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1107(int x) => (x * 1110) + 1107;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1108(int x) => (x * 1111) + 1108;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1109(int x) => (x * 1112) + 1109;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1110(int x) => (x * 1113) + 1110;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1111(int x) => (x * 1114) + 1111;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1112(int x) => (x * 1115) + 1112;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1113(int x) => (x * 1116) + 1113;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1114(int x) => (x * 1117) + 1114;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1115(int x) => (x * 1118) + 1115;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1116(int x) => (x * 1119) + 1116;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1117(int x) => (x * 1120) + 1117;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1118(int x) => (x * 1121) + 1118;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1119(int x) => (x * 1122) + 1119;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1120(int x) => (x * 1123) + 1120;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1121(int x) => (x * 1124) + 1121;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1122(int x) => (x * 1125) + 1122;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1123(int x) => (x * 1126) + 1123;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1124(int x) => (x * 1127) + 1124;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1125(int x) => (x * 1128) + 1125;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1126(int x) => (x * 1129) + 1126;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1127(int x) => (x * 1130) + 1127;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1128(int x) => (x * 1131) + 1128;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1129(int x) => (x * 1132) + 1129;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1130(int x) => (x * 1133) + 1130;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1131(int x) => (x * 1134) + 1131;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1132(int x) => (x * 1135) + 1132;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1133(int x) => (x * 1136) + 1133;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1134(int x) => (x * 1137) + 1134;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1135(int x) => (x * 1138) + 1135;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1136(int x) => (x * 1139) + 1136;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1137(int x) => (x * 1140) + 1137;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1138(int x) => (x * 1141) + 1138;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1139(int x) => (x * 1142) + 1139;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1140(int x) => (x * 1143) + 1140;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1141(int x) => (x * 1144) + 1141;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1142(int x) => (x * 1145) + 1142;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1143(int x) => (x * 1146) + 1143;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1144(int x) => (x * 1147) + 1144;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1145(int x) => (x * 1148) + 1145;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1146(int x) => (x * 1149) + 1146;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1147(int x) => (x * 1150) + 1147;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1148(int x) => (x * 1151) + 1148;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1149(int x) => (x * 1152) + 1149;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1150(int x) => (x * 1153) + 1150;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1151(int x) => (x * 1154) + 1151;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1152(int x) => (x * 1155) + 1152;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1153(int x) => (x * 1156) + 1153;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1154(int x) => (x * 1157) + 1154;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1155(int x) => (x * 1158) + 1155;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1156(int x) => (x * 1159) + 1156;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1157(int x) => (x * 1160) + 1157;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1158(int x) => (x * 1161) + 1158;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1159(int x) => (x * 1162) + 1159;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1160(int x) => (x * 1163) + 1160;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1161(int x) => (x * 1164) + 1161;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1162(int x) => (x * 1165) + 1162;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1163(int x) => (x * 1166) + 1163;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1164(int x) => (x * 1167) + 1164;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1165(int x) => (x * 1168) + 1165;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1166(int x) => (x * 1169) + 1166;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1167(int x) => (x * 1170) + 1167;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1168(int x) => (x * 1171) + 1168;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1169(int x) => (x * 1172) + 1169;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1170(int x) => (x * 1173) + 1170;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1171(int x) => (x * 1174) + 1171;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1172(int x) => (x * 1175) + 1172;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1173(int x) => (x * 1176) + 1173;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1174(int x) => (x * 1177) + 1174;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1175(int x) => (x * 1178) + 1175;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1176(int x) => (x * 1179) + 1176;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1177(int x) => (x * 1180) + 1177;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1178(int x) => (x * 1181) + 1178;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1179(int x) => (x * 1182) + 1179;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1180(int x) => (x * 1183) + 1180;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1181(int x) => (x * 1184) + 1181;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1182(int x) => (x * 1185) + 1182;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1183(int x) => (x * 1186) + 1183;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1184(int x) => (x * 1187) + 1184;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1185(int x) => (x * 1188) + 1185;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1186(int x) => (x * 1189) + 1186;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1187(int x) => (x * 1190) + 1187;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1188(int x) => (x * 1191) + 1188;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1189(int x) => (x * 1192) + 1189;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1190(int x) => (x * 1193) + 1190;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1191(int x) => (x * 1194) + 1191;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1192(int x) => (x * 1195) + 1192;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1193(int x) => (x * 1196) + 1193;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1194(int x) => (x * 1197) + 1194;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1195(int x) => (x * 1198) + 1195;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1196(int x) => (x * 1199) + 1196;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1197(int x) => (x * 1200) + 1197;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1198(int x) => (x * 1201) + 1198;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1199(int x) => (x * 1202) + 1199;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1200(int x) => (x * 1203) + 1200;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1201(int x) => (x * 1204) + 1201;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1202(int x) => (x * 1205) + 1202;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1203(int x) => (x * 1206) + 1203;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1204(int x) => (x * 1207) + 1204;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1205(int x) => (x * 1208) + 1205;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1206(int x) => (x * 1209) + 1206;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1207(int x) => (x * 1210) + 1207;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1208(int x) => (x * 1211) + 1208;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1209(int x) => (x * 1212) + 1209;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1210(int x) => (x * 1213) + 1210;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1211(int x) => (x * 1214) + 1211;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1212(int x) => (x * 1215) + 1212;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1213(int x) => (x * 1216) + 1213;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1214(int x) => (x * 1217) + 1214;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1215(int x) => (x * 1218) + 1215;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1216(int x) => (x * 1219) + 1216;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1217(int x) => (x * 1220) + 1217;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1218(int x) => (x * 1221) + 1218;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1219(int x) => (x * 1222) + 1219;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1220(int x) => (x * 1223) + 1220;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1221(int x) => (x * 1224) + 1221;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1222(int x) => (x * 1225) + 1222;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1223(int x) => (x * 1226) + 1223;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1224(int x) => (x * 1227) + 1224;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1225(int x) => (x * 1228) + 1225;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1226(int x) => (x * 1229) + 1226;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1227(int x) => (x * 1230) + 1227;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1228(int x) => (x * 1231) + 1228;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1229(int x) => (x * 1232) + 1229;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1230(int x) => (x * 1233) + 1230;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1231(int x) => (x * 1234) + 1231;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1232(int x) => (x * 1235) + 1232;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1233(int x) => (x * 1236) + 1233;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1234(int x) => (x * 1237) + 1234;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1235(int x) => (x * 1238) + 1235;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1236(int x) => (x * 1239) + 1236;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1237(int x) => (x * 1240) + 1237;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1238(int x) => (x * 1241) + 1238;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1239(int x) => (x * 1242) + 1239;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1240(int x) => (x * 1243) + 1240;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1241(int x) => (x * 1244) + 1241;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1242(int x) => (x * 1245) + 1242;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1243(int x) => (x * 1246) + 1243;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1244(int x) => (x * 1247) + 1244;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1245(int x) => (x * 1248) + 1245;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1246(int x) => (x * 1249) + 1246;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1247(int x) => (x * 1250) + 1247;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1248(int x) => (x * 1251) + 1248;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1249(int x) => (x * 1252) + 1249;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1250(int x) => (x * 1253) + 1250;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1251(int x) => (x * 1254) + 1251;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1252(int x) => (x * 1255) + 1252;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1253(int x) => (x * 1256) + 1253;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1254(int x) => (x * 1257) + 1254;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1255(int x) => (x * 1258) + 1255;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1256(int x) => (x * 1259) + 1256;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1257(int x) => (x * 1260) + 1257;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1258(int x) => (x * 1261) + 1258;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1259(int x) => (x * 1262) + 1259;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1260(int x) => (x * 1263) + 1260;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1261(int x) => (x * 1264) + 1261;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1262(int x) => (x * 1265) + 1262;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1263(int x) => (x * 1266) + 1263;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1264(int x) => (x * 1267) + 1264;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1265(int x) => (x * 1268) + 1265;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1266(int x) => (x * 1269) + 1266;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1267(int x) => (x * 1270) + 1267;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1268(int x) => (x * 1271) + 1268;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1269(int x) => (x * 1272) + 1269;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1270(int x) => (x * 1273) + 1270;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1271(int x) => (x * 1274) + 1271;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1272(int x) => (x * 1275) + 1272;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1273(int x) => (x * 1276) + 1273;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1274(int x) => (x * 1277) + 1274;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1275(int x) => (x * 1278) + 1275;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1276(int x) => (x * 1279) + 1276;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1277(int x) => (x * 1280) + 1277;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1278(int x) => (x * 1281) + 1278;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1279(int x) => (x * 1282) + 1279;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1280(int x) => (x * 1283) + 1280;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1281(int x) => (x * 1284) + 1281;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1282(int x) => (x * 1285) + 1282;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1283(int x) => (x * 1286) + 1283;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1284(int x) => (x * 1287) + 1284;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1285(int x) => (x * 1288) + 1285;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1286(int x) => (x * 1289) + 1286;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1287(int x) => (x * 1290) + 1287;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1288(int x) => (x * 1291) + 1288;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1289(int x) => (x * 1292) + 1289;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1290(int x) => (x * 1293) + 1290;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1291(int x) => (x * 1294) + 1291;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1292(int x) => (x * 1295) + 1292;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1293(int x) => (x * 1296) + 1293;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1294(int x) => (x * 1297) + 1294;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1295(int x) => (x * 1298) + 1295;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1296(int x) => (x * 1299) + 1296;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1297(int x) => (x * 1300) + 1297;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1298(int x) => (x * 1301) + 1298;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1299(int x) => (x * 1302) + 1299;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1300(int x) => (x * 1303) + 1300;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1301(int x) => (x * 1304) + 1301;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1302(int x) => (x * 1305) + 1302;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1303(int x) => (x * 1306) + 1303;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1304(int x) => (x * 1307) + 1304;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1305(int x) => (x * 1308) + 1305;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1306(int x) => (x * 1309) + 1306;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1307(int x) => (x * 1310) + 1307;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1308(int x) => (x * 1311) + 1308;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1309(int x) => (x * 1312) + 1309;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1310(int x) => (x * 1313) + 1310;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1311(int x) => (x * 1314) + 1311;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1312(int x) => (x * 1315) + 1312;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1313(int x) => (x * 1316) + 1313;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1314(int x) => (x * 1317) + 1314;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1315(int x) => (x * 1318) + 1315;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1316(int x) => (x * 1319) + 1316;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1317(int x) => (x * 1320) + 1317;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1318(int x) => (x * 1321) + 1318;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1319(int x) => (x * 1322) + 1319;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1320(int x) => (x * 1323) + 1320;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1321(int x) => (x * 1324) + 1321;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1322(int x) => (x * 1325) + 1322;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1323(int x) => (x * 1326) + 1323;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1324(int x) => (x * 1327) + 1324;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1325(int x) => (x * 1328) + 1325;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1326(int x) => (x * 1329) + 1326;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1327(int x) => (x * 1330) + 1327;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1328(int x) => (x * 1331) + 1328;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1329(int x) => (x * 1332) + 1329;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1330(int x) => (x * 1333) + 1330;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1331(int x) => (x * 1334) + 1331;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1332(int x) => (x * 1335) + 1332;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1333(int x) => (x * 1336) + 1333;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1334(int x) => (x * 1337) + 1334;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1335(int x) => (x * 1338) + 1335;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1336(int x) => (x * 1339) + 1336;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1337(int x) => (x * 1340) + 1337;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1338(int x) => (x * 1341) + 1338;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1339(int x) => (x * 1342) + 1339;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1340(int x) => (x * 1343) + 1340;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1341(int x) => (x * 1344) + 1341;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1342(int x) => (x * 1345) + 1342;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1343(int x) => (x * 1346) + 1343;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1344(int x) => (x * 1347) + 1344;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1345(int x) => (x * 1348) + 1345;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1346(int x) => (x * 1349) + 1346;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1347(int x) => (x * 1350) + 1347;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1348(int x) => (x * 1351) + 1348;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1349(int x) => (x * 1352) + 1349;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1350(int x) => (x * 1353) + 1350;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1351(int x) => (x * 1354) + 1351;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1352(int x) => (x * 1355) + 1352;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1353(int x) => (x * 1356) + 1353;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1354(int x) => (x * 1357) + 1354;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1355(int x) => (x * 1358) + 1355;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1356(int x) => (x * 1359) + 1356;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1357(int x) => (x * 1360) + 1357;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1358(int x) => (x * 1361) + 1358;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1359(int x) => (x * 1362) + 1359;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1360(int x) => (x * 1363) + 1360;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1361(int x) => (x * 1364) + 1361;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1362(int x) => (x * 1365) + 1362;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1363(int x) => (x * 1366) + 1363;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1364(int x) => (x * 1367) + 1364;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1365(int x) => (x * 1368) + 1365;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1366(int x) => (x * 1369) + 1366;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1367(int x) => (x * 1370) + 1367;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1368(int x) => (x * 1371) + 1368;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1369(int x) => (x * 1372) + 1369;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1370(int x) => (x * 1373) + 1370;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1371(int x) => (x * 1374) + 1371;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1372(int x) => (x * 1375) + 1372;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1373(int x) => (x * 1376) + 1373;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1374(int x) => (x * 1377) + 1374;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1375(int x) => (x * 1378) + 1375;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1376(int x) => (x * 1379) + 1376;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1377(int x) => (x * 1380) + 1377;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1378(int x) => (x * 1381) + 1378;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1379(int x) => (x * 1382) + 1379;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1380(int x) => (x * 1383) + 1380;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1381(int x) => (x * 1384) + 1381;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1382(int x) => (x * 1385) + 1382;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1383(int x) => (x * 1386) + 1383;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1384(int x) => (x * 1387) + 1384;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1385(int x) => (x * 1388) + 1385;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1386(int x) => (x * 1389) + 1386;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1387(int x) => (x * 1390) + 1387;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1388(int x) => (x * 1391) + 1388;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1389(int x) => (x * 1392) + 1389;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1390(int x) => (x * 1393) + 1390;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1391(int x) => (x * 1394) + 1391;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1392(int x) => (x * 1395) + 1392;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1393(int x) => (x * 1396) + 1393;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1394(int x) => (x * 1397) + 1394;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1395(int x) => (x * 1398) + 1395;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1396(int x) => (x * 1399) + 1396;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1397(int x) => (x * 1400) + 1397;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1398(int x) => (x * 1401) + 1398;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1399(int x) => (x * 1402) + 1399;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1400(int x) => (x * 1403) + 1400;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1401(int x) => (x * 1404) + 1401;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1402(int x) => (x * 1405) + 1402;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1403(int x) => (x * 1406) + 1403;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1404(int x) => (x * 1407) + 1404;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1405(int x) => (x * 1408) + 1405;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1406(int x) => (x * 1409) + 1406;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1407(int x) => (x * 1410) + 1407;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1408(int x) => (x * 1411) + 1408;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1409(int x) => (x * 1412) + 1409;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1410(int x) => (x * 1413) + 1410;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1411(int x) => (x * 1414) + 1411;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1412(int x) => (x * 1415) + 1412;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1413(int x) => (x * 1416) + 1413;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1414(int x) => (x * 1417) + 1414;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1415(int x) => (x * 1418) + 1415;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1416(int x) => (x * 1419) + 1416;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1417(int x) => (x * 1420) + 1417;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1418(int x) => (x * 1421) + 1418;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1419(int x) => (x * 1422) + 1419;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1420(int x) => (x * 1423) + 1420;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1421(int x) => (x * 1424) + 1421;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1422(int x) => (x * 1425) + 1422;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1423(int x) => (x * 1426) + 1423;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1424(int x) => (x * 1427) + 1424;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1425(int x) => (x * 1428) + 1425;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1426(int x) => (x * 1429) + 1426;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1427(int x) => (x * 1430) + 1427;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1428(int x) => (x * 1431) + 1428;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1429(int x) => (x * 1432) + 1429;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1430(int x) => (x * 1433) + 1430;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1431(int x) => (x * 1434) + 1431;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1432(int x) => (x * 1435) + 1432;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1433(int x) => (x * 1436) + 1433;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1434(int x) => (x * 1437) + 1434;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1435(int x) => (x * 1438) + 1435;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1436(int x) => (x * 1439) + 1436;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1437(int x) => (x * 1440) + 1437;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1438(int x) => (x * 1441) + 1438;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1439(int x) => (x * 1442) + 1439;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1440(int x) => (x * 1443) + 1440;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1441(int x) => (x * 1444) + 1441;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1442(int x) => (x * 1445) + 1442;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1443(int x) => (x * 1446) + 1443;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1444(int x) => (x * 1447) + 1444;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1445(int x) => (x * 1448) + 1445;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1446(int x) => (x * 1449) + 1446;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1447(int x) => (x * 1450) + 1447;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1448(int x) => (x * 1451) + 1448;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1449(int x) => (x * 1452) + 1449;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1450(int x) => (x * 1453) + 1450;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1451(int x) => (x * 1454) + 1451;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1452(int x) => (x * 1455) + 1452;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1453(int x) => (x * 1456) + 1453;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1454(int x) => (x * 1457) + 1454;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1455(int x) => (x * 1458) + 1455;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1456(int x) => (x * 1459) + 1456;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1457(int x) => (x * 1460) + 1457;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1458(int x) => (x * 1461) + 1458;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1459(int x) => (x * 1462) + 1459;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1460(int x) => (x * 1463) + 1460;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1461(int x) => (x * 1464) + 1461;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1462(int x) => (x * 1465) + 1462;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1463(int x) => (x * 1466) + 1463;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1464(int x) => (x * 1467) + 1464;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1465(int x) => (x * 1468) + 1465;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1466(int x) => (x * 1469) + 1466;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1467(int x) => (x * 1470) + 1467;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1468(int x) => (x * 1471) + 1468;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1469(int x) => (x * 1472) + 1469;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1470(int x) => (x * 1473) + 1470;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1471(int x) => (x * 1474) + 1471;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1472(int x) => (x * 1475) + 1472;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1473(int x) => (x * 1476) + 1473;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1474(int x) => (x * 1477) + 1474;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1475(int x) => (x * 1478) + 1475;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1476(int x) => (x * 1479) + 1476;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1477(int x) => (x * 1480) + 1477;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1478(int x) => (x * 1481) + 1478;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1479(int x) => (x * 1482) + 1479;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1480(int x) => (x * 1483) + 1480;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1481(int x) => (x * 1484) + 1481;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1482(int x) => (x * 1485) + 1482;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1483(int x) => (x * 1486) + 1483;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1484(int x) => (x * 1487) + 1484;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1485(int x) => (x * 1488) + 1485;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1486(int x) => (x * 1489) + 1486;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1487(int x) => (x * 1490) + 1487;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1488(int x) => (x * 1491) + 1488;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1489(int x) => (x * 1492) + 1489;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1490(int x) => (x * 1493) + 1490;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1491(int x) => (x * 1494) + 1491;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1492(int x) => (x * 1495) + 1492;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1493(int x) => (x * 1496) + 1493;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1494(int x) => (x * 1497) + 1494;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1495(int x) => (x * 1498) + 1495;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1496(int x) => (x * 1499) + 1496;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1497(int x) => (x * 1500) + 1497;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1498(int x) => (x * 1501) + 1498;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1499(int x) => (x * 1502) + 1499;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1500(int x) => (x * 1503) + 1500;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1501(int x) => (x * 1504) + 1501;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1502(int x) => (x * 1505) + 1502;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1503(int x) => (x * 1506) + 1503;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1504(int x) => (x * 1507) + 1504;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1505(int x) => (x * 1508) + 1505;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1506(int x) => (x * 1509) + 1506;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1507(int x) => (x * 1510) + 1507;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1508(int x) => (x * 1511) + 1508;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1509(int x) => (x * 1512) + 1509;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1510(int x) => (x * 1513) + 1510;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1511(int x) => (x * 1514) + 1511;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1512(int x) => (x * 1515) + 1512;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1513(int x) => (x * 1516) + 1513;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1514(int x) => (x * 1517) + 1514;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1515(int x) => (x * 1518) + 1515;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1516(int x) => (x * 1519) + 1516;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1517(int x) => (x * 1520) + 1517;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1518(int x) => (x * 1521) + 1518;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1519(int x) => (x * 1522) + 1519;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1520(int x) => (x * 1523) + 1520;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1521(int x) => (x * 1524) + 1521;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1522(int x) => (x * 1525) + 1522;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1523(int x) => (x * 1526) + 1523;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1524(int x) => (x * 1527) + 1524;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1525(int x) => (x * 1528) + 1525;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1526(int x) => (x * 1529) + 1526;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1527(int x) => (x * 1530) + 1527;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1528(int x) => (x * 1531) + 1528;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1529(int x) => (x * 1532) + 1529;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1530(int x) => (x * 1533) + 1530;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1531(int x) => (x * 1534) + 1531;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1532(int x) => (x * 1535) + 1532;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1533(int x) => (x * 1536) + 1533;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1534(int x) => (x * 1537) + 1534;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1535(int x) => (x * 1538) + 1535;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1536(int x) => (x * 1539) + 1536;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1537(int x) => (x * 1540) + 1537;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1538(int x) => (x * 1541) + 1538;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1539(int x) => (x * 1542) + 1539;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1540(int x) => (x * 1543) + 1540;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1541(int x) => (x * 1544) + 1541;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1542(int x) => (x * 1545) + 1542;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1543(int x) => (x * 1546) + 1543;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1544(int x) => (x * 1547) + 1544;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1545(int x) => (x * 1548) + 1545;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1546(int x) => (x * 1549) + 1546;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1547(int x) => (x * 1550) + 1547;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1548(int x) => (x * 1551) + 1548;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1549(int x) => (x * 1552) + 1549;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1550(int x) => (x * 1553) + 1550;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1551(int x) => (x * 1554) + 1551;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1552(int x) => (x * 1555) + 1552;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1553(int x) => (x * 1556) + 1553;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1554(int x) => (x * 1557) + 1554;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1555(int x) => (x * 1558) + 1555;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1556(int x) => (x * 1559) + 1556;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1557(int x) => (x * 1560) + 1557;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1558(int x) => (x * 1561) + 1558;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1559(int x) => (x * 1562) + 1559;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1560(int x) => (x * 1563) + 1560;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1561(int x) => (x * 1564) + 1561;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1562(int x) => (x * 1565) + 1562;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1563(int x) => (x * 1566) + 1563;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1564(int x) => (x * 1567) + 1564;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1565(int x) => (x * 1568) + 1565;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1566(int x) => (x * 1569) + 1566;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1567(int x) => (x * 1570) + 1567;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1568(int x) => (x * 1571) + 1568;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1569(int x) => (x * 1572) + 1569;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1570(int x) => (x * 1573) + 1570;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1571(int x) => (x * 1574) + 1571;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1572(int x) => (x * 1575) + 1572;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1573(int x) => (x * 1576) + 1573;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1574(int x) => (x * 1577) + 1574;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1575(int x) => (x * 1578) + 1575;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1576(int x) => (x * 1579) + 1576;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1577(int x) => (x * 1580) + 1577;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1578(int x) => (x * 1581) + 1578;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1579(int x) => (x * 1582) + 1579;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1580(int x) => (x * 1583) + 1580;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1581(int x) => (x * 1584) + 1581;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1582(int x) => (x * 1585) + 1582;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1583(int x) => (x * 1586) + 1583;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1584(int x) => (x * 1587) + 1584;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1585(int x) => (x * 1588) + 1585;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1586(int x) => (x * 1589) + 1586;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1587(int x) => (x * 1590) + 1587;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1588(int x) => (x * 1591) + 1588;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1589(int x) => (x * 1592) + 1589;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1590(int x) => (x * 1593) + 1590;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1591(int x) => (x * 1594) + 1591;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1592(int x) => (x * 1595) + 1592;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1593(int x) => (x * 1596) + 1593;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1594(int x) => (x * 1597) + 1594;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1595(int x) => (x * 1598) + 1595;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1596(int x) => (x * 1599) + 1596;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1597(int x) => (x * 1600) + 1597;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1598(int x) => (x * 1601) + 1598;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int N1599(int x) => (x * 1602) + 1599;
}
