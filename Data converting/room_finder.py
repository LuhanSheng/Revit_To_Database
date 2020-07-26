# coding: utf-8
import sys
import math



def welcome(name):
    """ Script test """

    return "Loading script successfully ! ! !, " + name


def printList(list):
    s = ""
    for i in list:
        s += str(i.X) + str(i.Y) + str(i.Z)
    return s


def get_distance(point1, point2):
    """ Get the square of the distance of two points """

    distance = (point1.X - point2.X) ** 2 + (point1.Y - point2.Y) ** 2 + (point1.Z - point2.Z) ** 2
    return distance


def get_Nearest_Rooms(point, room_ps):
    """ Get the nearest four rooms """

    wp = open('C:/Users/Neo/Desktop/Summer research/point.out', 'w')
    wp.write(str(point.X) + " " + str(point.Y) + "\n")
    w = open('C:/Users/Neo/Desktop/Summer research/room1.out', 'w')
    for room_p in room_ps:
        if room_p[0].Z > 10:
            corner_points = []
            for i in range(len(room_p)):
                if i == 0:
                    continue
                corner_points.append(room_p[i])
            for i in range(len(corner_points) - 1):
                w.write(str(corner_points[i].X) + " " + str(corner_points[i].Y) + "\n")
                w.write(str(corner_points[i + 1].X) + " " + str(corner_points[i + 1].Y) + "\n")
                w.write("\n")
            w.write(str(corner_points[-1].X) + " " + str(corner_points[-1].Y) + "\n")
            w.write(str(corner_points[0].X) + " " + str(corner_points[0].Y) + "\n")
            w.write("\n")

    print("There are " + str(len(room_ps)) + " rooms in all, here are their distances between the target point")
    distances = []
    i = 0
    for room_p in room_ps:
        distance = get_distance(point, room_p[0])
        print("Index: " + str(i) + "   Distance: " + str(distance) + " inches")
        i = i + 1
        distances.append(distance)
    distances_sorted = sorted(distances)
    indexes = []
    for distance in distances_sorted[0:4]:
        index = distances.index(distance)
        indexes.append(index)
    print(indexes)
    return indexes


def point_Not_In_List(point, list):
    """ Whether a point is in the list"""

    for l in list:
        if abs(point[0] - l[0]) < 1 and abs(point[1] - l[1]) < 1:
            return False
    return True


def isRayIntersectsSegment(poi, s_poi, e_poi):  # [x,y] [lng,lat]
    """
        Whether the point is in the simple poly

        point；simple poly list；tolerance
        simPoly=[[x1,y1],[x2,y2],……,[xn,yn],[x1,y1]]

    # input：target point，start point of the edge，end point of the edge，in [lng,lat] format

    # Exclude the case that the ray is parallel and coincident,
    # and exclude the case that the first and last ends of the line segment are coincident
    """
    if s_poi[1] == e_poi[1]:
        return False
    if s_poi[1] > poi[1] and e_poi[1] > poi[1]:  # Line segment above ray
        return False
    if s_poi[1] < poi[1] and e_poi[1] < poi[1]:  # Line segment under ray
        return False
    if s_poi[1] == poi[1] and e_poi[1] > poi[1]:  # Intersection is the lower end，corresponding to spoint
        return False
    if e_poi[1] == poi[1] and s_poi[1] > poi[1]:  # Intersection is the lower end，corresponding to epoint
        return False
    if s_poi[0] < poi[0] and e_poi[1] < poi[1]:  # The line segment is to the left of the ray
        return False

    xseg = e_poi[0] - (e_poi[0] - s_poi[0]) * (e_poi[1] - poi[1]) / (e_poi[1] - s_poi[1])  # Find the intersection
    if xseg < poi[0]:
        return False
    return True


def isPoiWithinSimplePoly(poi1, simPoly1, tolerance=0.0001):
    """
        Whether the point is in the simple poly

        point；simple poly list；tolerance
        simPoly=[[x1,y1],[x2,y2],……,[xn,yn],[x1,y1]]
    """
    simPoly = []
    poi = []
    for a in simPoly1:
        simPoly.append(a)
    for b in poi1:
        poi.append(b)

    print(poi)
    print(simPoly)
    x_smallest = 1000000
    y_smallest = 1000000
    for s in simPoly:
        if x_smallest > s[0]:
            x_smallest = s[0]
        if y_smallest > s[1]:
            y_smallest = s[1]
    print(x_smallest, y_smallest)
    if x_smallest < 0:
        for i in simPoly:
            i[0] -= x_smallest
        poi[0] -= x_smallest
    if y_smallest < 0:
        for i in simPoly:
            i[1] -= y_smallest
        poi[1] -= y_smallest

    simPoly.append(simPoly[0])

    polylen = len(simPoly)
    sinsc = 0  # number of intersections
    for i in range(polylen - 1):
        s_poi = simPoly[i]
        e_poi = simPoly[i + 1]
        if isRayIntersectsSegment(poi, s_poi, e_poi):
            sinsc += 1

    print(sinsc)
    return True if sinsc % 2 == 1 else False


def isPoiWithinPoly(poi, poly):
    # poly=[[[x1,y1],[x2,y2],……,[xn,yn],[x1,y1]],[[w1,t1],……[wk,tk]]] three dimension array

    sinsc = 0
    for epoly in poly:
        for i in range(len(epoly) - 1):  # [0,len-1]
            s_poi = epoly[i]
            e_poi = epoly[i + 1]
            if isRayIntersectsSegment(poi, s_poi, e_poi):
                sinsc += 1

    return True if sinsc % 2 == 1 else False


def drawWindowLocations(windowLocations):
    w = open('C:/Users/Neo/Desktop/Summer research/window.out', 'w')
    for WindowLocation in windowLocations:
        # if WindowLocation.Z > 10:
        w.write(str(WindowLocation.X) + " " + str(WindowLocation.Y) + "\n")


def storeWindowData(windowData):
    w = open('C:/Users/Neo/Desktop/Summer research/window.txt', 'w')
    w.write("building_id room_name lat long height" + "\n")
    w1 = open('C:/Users/Neo/Desktop/Summer research/window1.out', 'w')
    for data in windowData:
        # if WindowLocation.Z > 10:
        w.write(str(data[0]) + " " + str(data[1]) + " " + str(data[2]) + " " + str(data[3]) + " " + str(data[4]) + "\n")
        if data[4] > 10:
            w1.write(str(data[2]) + " " + str(data[3]) + "\n")

def getCoordinate(survey_point, target_point, bias):
    pi = math.pi
    latitude = survey_point[0]
    perimeter = math.cos(abs(latitude)/180) * 40076000
    # print(perimeter)
    unit_longitude = perimeter/360 * 3.28083989501
    unit_latitude = 111322.222222222 * 3.28083989501

    angle = bias[3]
    if angle == 0:
        base_x = target_point[0]
        base_y = target_point[1]
    elif angle <= pi/2:
        y_xlxc = target_point[0] * (-1) + target_point[1] * 1 / math.tan(angle)
        x_xlxc = target_point[0] * 1 + target_point[1] * math.tan(angle)

        x = abs((1 / math.tan(angle) * target_point[0] + target_point[1]) / ((1 + (1 / math.tan(angle)) ** 2) ** 0.5))
        y = abs((-math.tan(angle)*target_point[0] + target_point[1])/((1 + math.tan(angle)**2)**0.5))
        # print(x, y)
        if y_xlxc >= 0 and x_xlxc >= 0:
            base_x = x
            base_y = y
        elif y_xlxc >= 0 and x_xlxc <= 0:
            base_x = -x
            base_y = y
        elif y_xlxc <= 0 and x_xlxc >= 0:
            base_x = x
            base_y = -y
        else:
            base_x = -x
            base_y = -y
    elif angle <= pi:
        angle = angle - pi/2
        x_xlxc = target_point[0] * (-1) + target_point[1] * 1 / math.tan(angle)
        y_xlxc = target_point[0] * (-1) + target_point[1] * (-math.tan(angle))

        y = abs((1 / math.tan(angle) * target_point[0] + target_point[1]) / ((1 + (1 / math.tan(angle)) ** 2) ** 0.5))
        x = abs((-math.tan(angle) * target_point[0] + target_point[1]) / ((1 + math.tan(angle) ** 2) ** 0.5))
        print(x_xlxc,y_xlxc)
        print(x, y)
        if y_xlxc >= 0 and x_xlxc >= 0:
            base_x = x
            base_y = y
        elif y_xlxc >= 0 and x_xlxc <= 0:
            base_x = -x
            base_y = y
        elif y_xlxc <= 0 and x_xlxc >= 0:
            base_x = x
            base_y = -y
        else:
            base_x = -x
            base_y = -y
    elif angle <= 3/2*pi:
        angle = angle - pi
        y_xlxc = target_point[0] * 1 + target_point[1] * (-1 / math.tan(angle))
        x_xlxc = target_point[0] * (-1) + target_point[1] * (-math.tan(angle))

        x = abs((1 / math.tan(angle) * target_point[0] + target_point[1]) / ((1 + (1 / math.tan(angle)) ** 2) ** 0.5))
        y = abs((-math.tan(angle) * target_point[0] + target_point[1]) / ((1 + math.tan(angle) ** 2) ** 0.5))
        print(x_xlxc,y_xlxc)
        print(x, y)
        if y_xlxc >= 0 and x_xlxc >= 0:
            base_x = x
            base_y = y
        elif y_xlxc >= 0 and x_xlxc <= 0:
            base_x = -x
            base_y = y
        elif y_xlxc <= 0 and x_xlxc >= 0:
            base_x = x
            base_y = -y
        else:
            base_x = -x
            base_y = -y
    else:
        angle = angle - 3/2*pi
        x_xlxc = target_point[0] * 1 + target_point[1] * (-1 / math.tan(angle))
        y_xlxc = target_point[0] * 1 + target_point[1] * math.tan(angle)

        y = abs((1 / math.tan(angle) * target_point[0] + target_point[1]) / ((1 + (1 / math.tan(angle)) ** 2) ** 0.5))
        x = abs((-math.tan(angle) * target_point[0] + target_point[1]) / ((1 + math.tan(angle) ** 2) ** 0.5))
        print(x_xlxc, y_xlxc)
        print(x, y)
        if y_xlxc >= 0 and x_xlxc >= 0:
            base_x = x
            base_y = y
        elif y_xlxc >= 0 and x_xlxc <= 0:
            base_x = -x
            base_y = y
        elif y_xlxc <= 0 and x_xlxc >= 0:
            base_x = x
            base_y = -y
        else:
            base_x = -x
            base_y = -y

    long = (base_x + bias[0])/unit_longitude
    lat = (base_y + bias[1])/unit_latitude
    # print(unit_longitude,unit_latitude)
    # print(long, lat)

    print(str(long + survey_point[1]) + " " + str(lat + survey_point[0]))
    return [lat + survey_point[0], long + survey_point[1], target_point[2]*0.3048]

if __name__ == "__main__":
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