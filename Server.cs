using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace server_side
{ 
    class Server
    {
        static Socket ServerSoket;
        public static List<Users> UserList = new List<Users>();
        static Thread listenerThread;
        public static int countUsers = 0;
        static void Main()
        {
            try
            {
                bool work = true;
                //запуск
                Console.Write("Введите порт сервера: ");
                IPAddress add =  GetLocalIPAddress();
                int port = int.Parse(Console.ReadLine());
                ServerSoket = new Socket(add.AddressFamily, SocketType.Stream,ProtocolType.Tcp);
                ServerSoket.Bind(new IPEndPoint(add, port));
                ServerSoket.Listen(10);
                Console.WriteLine("Сервер запущен по адресу: "+ServerSoket.LocalEndPoint);
                Console.WriteLine("Ожидание подключений...");
                //прослушка
                listenerThread = new Thread(listening);
                listenerThread.Start();
                do
                {
                    string command = Console.ReadLine();
                    switch (command)
                    {
                        case "test":
                            Console.WriteLine("test+test");
                            break;
                        case "clients":
                            ShowClients();
                            break;
                        case "stop":
                            work = false; exit();
                            break;
                    }
                } while (work);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: "+ex.Message);
            }
        }
        static void exit()
        {
            ServerSoket.Dispose();
            ServerSoket.Close();
            listenerThread.Abort();
        }
        private static void ShowClients()
        {
            Console.WriteLine("Список пользователей:");
            for (int i = 0; i < countUsers; i++)
            {
                try
                {
                    string blok = "null";
                    if (UserList[i].block)
                        blok = "занят";
                    else blok = "свободен";
                    Console.WriteLine("Пользователь " + UserList[i].Username
                        + " c адресом " + UserList[i].ip_user
                        + " статус: " + blok);
                }
                catch { }
            }
        }
        //прослушка
        static void listening()
        {
            while (true)
            {
                try
                {
                    Socket handler = ServerSoket.Accept();
                    Console.WriteLine("Новое соединение");
                    new Users(handler);
                }
                catch { }
            }
        }
        //получить локальный адрес
        public static IPAddress GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }
            Console.WriteLine("Не обнаружены адаптеры сети IPv4");
            throw null;
        }
        //отключиться от клиента
        public static void EndUser(Users usr)
        {
            Console.WriteLine("Пользователь " + usr.Username + " отключился");
            countUsers--;
            if (!UserList.Contains(usr))
                return;
            usr.block = false;
            UserList.Remove(usr);//удаляем из списка
            usr.End();//закрываем сокет
        }
        //новый пользователь
        public static void NewUser(Users usr)
        {
            if (UserList.Contains(usr))
                return;
            UserList.Add(usr);
            countUsers++;
            Console.WriteLine("Пользователь: " + usr.Username +" с адресом "+usr.ip_user+" подключился ");
        }
        //поиск пользователя
        public static Users GetUser(string Name)
        {
            for (int i = 0; i < countUsers; i++)
            {
                if (UserList[i].Username == Name)
                    return UserList[i];
            }
            return null;
        }
    }
}