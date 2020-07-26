# survey_point = [39.9578285217285, -75.1940307617188]
# target_point = [-100, -100, 5]
# bias = [0, 0, 0, 0.13]
#
# print(getCoordinate(survey_point, target_point, bias))
#
# # print(math.pi)
# # print(sys.path)
import pymysql

# tolerance unit: meter
tolerance = 10
degree_tolerance = tolerance / 40000000 * 360

# lat, long, height in meter
fire_point = [39.916, 116.433, 0]

lat_upper_bound = fire_point[0] + degree_tolerance
lat_lower_bound = fire_point[0] - degree_tolerance

long_upper_bound = fire_point[1] + degree_tolerance
long_lower_bound = fire_point[1] - degree_tolerance

print(degree_tolerance)
print(lat_lower_bound, lat_upper_bound)
sql = "SELECT * FROM window.window WHERE (longitude BETWEEN " + str(long_lower_bound) + " AND " + str(
    long_upper_bound) + ") AND (latitude BETWEEN " + str(lat_lower_bound) + " AND " + str(lat_upper_bound) + ");"
# " AND (long BETWEEN " + str(lat_lower_bound)+ " AND " + str(lat_upper_bound)+
# long BETWEEN " + str(lat_lower_bound)+ " AND " + str(lat_upper_bound)+
print(sql)

conn = pymysql.connect(host='127.0.0.1',
                       port=3306,
                       user='root',
                       password='0106259685',
                       db='window',
                       charset='utf8',
                       )
cur = conn.cursor()
sqlstring = "SELECT * FROM window;"
cur.execute(sql)
results = cur.fetchall()

distances = []
if len(results) == 0:
    print("Did not find")
else:
    print(len(results), "possible rooms")
    for result in results:
        distance = (abs(fire_point[0] - result[3]) * 40000000 / 360) ** 2 + (
                    abs(fire_point[1] - result[4]) * 40000000 / 360) ** 2 + (abs(fire_point[2] - result[5])) ** 2
        distances.append(distance)
        print(distance, result[2])
    index = distances.index(min(distances))
    print(results[index])