
import pymysql
import sys

# database config
HOST = '127.0.0.1'
PORT = 3306
USER = 'root'
PASSWORD = '0106259685'
DATABASE = 'rescue'

# tolerance unit: meter
tolerance = 10
degree_tolerance = tolerance / 40000000 * 360

# lat, long, height in meter
fire_point = [float(sys.argv[1]), float(sys.argv[2]), float(sys.argv[3])]

lat_upper_bound = fire_point[0] + degree_tolerance
lat_lower_bound = fire_point[0] - degree_tolerance

long_upper_bound = fire_point[1] + degree_tolerance
long_lower_bound = fire_point[1] - degree_tolerance

sql = "SELECT * FROM window.window WHERE (longitude BETWEEN " + str(long_lower_bound) + " AND " + str(
    long_upper_bound) + ") AND (latitude BETWEEN " + str(lat_lower_bound) + " AND " + str(lat_upper_bound) + ");"
# " AND (long BETWEEN " + str(lat_lower_bound)+ " AND " + str(lat_upper_bound)+
# long BETWEEN " + str(lat_lower_bound)+ " AND " + str(lat_upper_bound)+

conn = pymysql.connect(host=HOST,
                       port=PORT,
                       user=USER,
                       password=PASSWORD,
                       db=DATABASE,
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
    for result in results:
        distance = (abs(fire_point[0] - result[3]) * 40000000 / 360) ** 2 + (
                abs(fire_point[1] - result[4]) * 40000000 / 360) ** 2 + (abs(fire_point[2] - result[5])) ** 2
        distances.append(distance)
    index = distances.index(min(distances))
    print("Building name: " + results[index][1])
    print("Room name: " + results[index][2])
    print("Window position: (" + str(results[index][3]) + ", " + str(results[index][4]) + ", " + str(results[index][5]) + ")")


