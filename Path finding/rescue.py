import time

import pymysql
import matplotlib.pyplot as plt

# database config
HOST = '127.0.0.1'
PORT = 3306
USER = 'root'
PASSWORD = '0106259685'
DATABASE = 'rescue'

# building config
FIRE_ROOM_NAME = "SCIENCE 250 | LEVEL 02"
BUILDING_NAME = "1507_DREXEL PSLAMS_CENTRAL_190327"

inf = float("inf")

class Graph:
    def __init__(self, result):
        self.vertexn = len(result)
        self.adjmatrix = [[inf] * self.vertexn for i in range(self.vertexn)]
        self.nodes = []
        for node in result:
            self.nodes.append(node[0])

    def addNode(self, edge):
        edge1 = self.nodes.index(edge[0])
        edge2 = self.nodes.index(edge[1])
        self.adjmatrix[edge1][edge2] = edge[2]
        self.adjmatrix[edge2][edge1] = edge[2]

    def print_graph(self):
        print(self.adjmatrix)

    def print_nodes(self):
        print(self.nodes)

    def Dijkstra(self, v0):
        # initialize
        D = [inf] * self.vertexn  # 用于存放从顶点v0到v的最短路径长度
        path = [None] * self.vertexn  # 用于存放从顶点v0到v的路径
        final = [None] * self.vertexn  # 表示从v0到v的最短路径是否找到最短路径

        for i in range(self.vertexn):
            final[i] = False
            D[i] = self.adjmatrix[v0][i]
            path[i] = ""  # 路径先置空
            if D[i] < inf:
                path[i] = self.nodes[i]  # 如果v0直接连到第i点，则路径直接改为i
        D[v0] = 0
        final[v0] = True
        ###
        for i in range(1, self.vertexn):
            min = inf  # 找到离v0最近的顶点
            for k in range(self.vertexn):
                if (not final[k]) and (D[k] < min):
                    v = k
                    min = D[k]
            final[v] = True  # 最近的点找到，加入到已得最短路径集合S中 此后的min将在处S以外的vertex中产生
            for k in range(self.vertexn):
                if (not final[k]) and (min + self.adjmatrix[v][k] < D[k]):
                    # 如果最短的距离(v0-v)加上v到k的距离小于现存v0到k的距离
                    D[k] = min + self.adjmatrix[v][k]
                    path[k] = path[v] + "," + self.nodes[k]
        return D, path

    def find_room(self, room_name):
        return self.nodes.index(room_name)

    def set_room_on_fire(self, room_name):
        room_index = self.nodes.index(room_name)
        for r in range(self.vertexn):
            self.adjmatrix[room_index][r] = inf

def find_edge(room1, room2):
    for edge in results1:
        if (room1 == edge[0] and room2 == edge[1]) or (room1 == edge[1] and room2 == edge[0]):
            if edge[3] == "door" and edge[4] != "null":
                return [edge[4].split(",")[0:2][0][1:], edge[4].split(",")[0:2][1]]
            else:
                return "null"

def paint_room(room_name, color):
    for wall in results3:
        if wall[0] == room_name:
            plt.plot([float(wall[1][1:-1].split(",")[0]), float(wall[2][1:-1].split(",")[0])],
                     [float(wall[1][1:-1].split(",")[1]), float(wall[2][1:-1].split(",")[1])], color=color)

def paint_map(map, map_number, destination):
    level = ""
    room_list = [-1]
    print("step " + str(map_number) + ":")
    for m in map:
        for p in m:
            if isinstance(p , int):
                level = results[p][0].split("|")[1]
                if len(room_list) > 0:
                    if p != room_list[-1]:
                        print(results[p][0].split("|")[0] + "--> ",end='')
                room_list.append(p)
    print(destination,end='')
    print()

    for wall in results3:
        if wall[0].split("|")[1] == level:
            plt.plot([float(wall[1][1:-1].split(",")[0]), float(wall[2][1:-1].split(",")[0])],
                     [float(wall[1][1:-1].split(",")[1]), float(wall[2][1:-1].split(",")[1])], color='black')

    for room_to_paint in room_list[1:]:
        paint_room(results[room_to_paint][0], "blue")

    for edge in map:
        if isinstance(edge[0], int) and isinstance(edge[1], int):
            plt.plot([float(results[edge[0]][1][1:-1].split(",")[0]), float(results[edge[1]][1][1:-1].split(",")[0])],
                    [float(results[edge[0]][1][1:-1].split(",")[1]), float(results[edge[1]][1][1:-1].split(",")[1])],
                     color='red')
        elif isinstance(edge[0], int):
            plt.plot([float(results[edge[0]][1][1:-1].split(",")[0]), float(edge[1][0])],
                     [float(results[edge[0]][1][1:-1].split(",")[1]), float(edge[1][1])],
                     color='red')
        elif isinstance(edge[1], int):
            plt.plot([float(edge[0][0]), float(results[edge[1]][1][1:-1].split(",")[0])],
                     [float(edge[0][1]), float(results[edge[1]][1][1:-1].split(",")[1])],
                     color='red')
        else:
            print("xxx")
    startPoint = map[0][0]
    endPoint = map[-1][1]

    if isinstance(startPoint, int):
        plt.text(float(results[startPoint][1][1:-1].split(",")[0])-8, float(results[startPoint][1][1:-1].split(",")[1]), 'START')
    else:
        plt.text(float(endPoint[0])-8, float(endPoint[1]), 'START')

    if isinstance(endPoint, int):
        plt.text(float(results[endPoint][1][1:-1].split(",")[0])-6, float(results[endPoint][1][1:-1].split(",")[1]), destination)
    else:
        plt.text(float(endPoint[0])-6, float(endPoint[1]), destination)

    plt.title("Step: " + str(map_number) + ", current level: " + level + ", please go to " + destination, size=20)
    plt.savefig("picture/" + "step " +str(map_number) + ".png")
    plt.clf()

if __name__ == "__main__":

    plt.figure(figsize=(12, 10))
    conn = pymysql.connect(host=HOST, port=PORT, user=USER, password=PASSWORD, db=DATABASE, charset='utf8')
    cur = conn.cursor()
    sqlstring = "SELECT room_name,room_location,is_exit,exit_location FROM rescue.node WHERE building_name='" + BUILDING_NAME + "';"
    cur.execute(sqlstring)
    results = cur.fetchall()

    sqlstring1 = "SELECT node1,node2,length,edge_type,edge_location FROM rescue.edge WHERE building_name='" + BUILDING_NAME + "';"
    cur.execute(sqlstring1)
    results1 = cur.fetchall()

    sqlstring2 = "SELECT room_name FROM rescue.fire WHERE building_name='" + BUILDING_NAME + "';"
    cur.execute(sqlstring2)
    results2 = cur.fetchall()

    sqlstring3 = "SELECT room_name,start_point,end_point FROM rescue.wall WHERE building_name='" + BUILDING_NAME + "';"
    cur.execute(sqlstring3)
    results3 = cur.fetchall()

    g = Graph(results)
    for edge in results1:
        g.addNode(edge)
    if len(results2) > 0:
        g.set_room_on_fire(results2[0][0])
    start = FIRE_ROOM_NAME
    index = g.find_room(start)
    D, path = g.Dijkstra(index)

    destination_index = -1
    destination_distance = inf
    for node in results:
        if node[2] == "true":
            index_exit = g.find_room(node[0])
            if D[index_exit] != inf:
                if D[index_exit] < destination_distance:
                    destination_distance = D[index_exit]
                    destination_index = index_exit

    path_list = path[destination_index].split(",")

    if len(path_list) == 1 and path_list[0] == '':
        path_list.clear()
    path_list.insert(0, start)

    exit_position = []
    for room in results:
        if room[0] == path_list[-1] and room[2] == "true":
            exit_position.append(room[3].split(",")[0][1:])
            exit_position.append(room[3].split(",")[1])

    picture_number = 1
    map = []

    if len(path_list) > 1:
        for j in range(len(path_list)-1):
            path = []
            i1 = g.find_room(path_list[j])
            i2 = g.find_room(path_list[j+1])

            temp_edge = find_edge(path_list[j], path_list[j + 1])
            if(results[i1][0].split("|")[1] == results[i2][0].split("|")[1]):
                if temp_edge == "null":
                    path.append(i1)
                    path.append(i2)
                    map.append(path)
                else:
                    path.append(i1)
                    path.append(temp_edge)
                    map.append(path)
                    path = []
                    path.append(temp_edge)
                    path.append(i2)
                    map.append(path)
            else:
                if len(map) == 0:
                    path.append(i1)
                    path.append(i1)
                    map.append(path)
                paint_map(map, picture_number, "STAIR")
                picture_number = picture_number + 1
                map.clear()


    if len(map) == 0:
        path = []
        i = g.find_room(path_list[-1])
        path.append(i)
        path.append(i)
        map.append(path)
    map.append([map[-1][1], exit_position])
    paint_map(map, picture_number, "EXIT")