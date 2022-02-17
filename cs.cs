using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;

class Program
{
    // Создаём словарь для сортировщика в формате (Ключ: папка, Значение: Список значений) для поиска совпадений в значениях и возврат ключа.
    internal static Dictionary<string, List<string>> types = new Dictionary<string, List<string>>();
    // Исходная позиция программы.
    static readonly string mainPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    // Относительный путь папки откуда перемещаются файлы (рядом с запускаемым файлом).
    static readonly string SourcePath = mainPath + "\\Source\\";
    // Относительный путь папки куда перемещаются файлы (рядом с запускаемым файлом).
    static readonly string DestPath = mainPath + "\\Dest";
    // Относительный путь папки куда пишутся логи, если найдены совпадения (рядом с запускаемым файлом).
    static readonly string LogsPath = mainPath + "\\Logs";

    // Очередь файлов. Первый элемент передаётся в первый поток.
    static Queue<string> AllValues = new Queue<string>(Directory.GetFiles(SourcePath));
    // Счётчик количества активных потоков.
    static int ActiveThreatingTasks = 0;

    /// <summary>
    /// Обновление очереди при добавление файла.
    /// </summary>
    /// <param name="Sender">Стандартный аргумент отправителя.</param>
    /// <param name="fileSystemEventArgs">Полученный файл.</param>
    static void Update(object Sender, FileSystemEventArgs fileSystemEventArgs)
    {
        // Добавление файла в конец очереди.
        AllValues.Enqueue(fileSystemEventArgs.FullPath);
    }

    private static void Main()
    {
        // Если нет папки Source - создаём.
        if (!Directory.Exists(SourcePath))
        {
            Directory.CreateDirectory(SourcePath);
        }

        // Если нет папки Dest - создаём.
        if (!Directory.Exists(DestPath))
        {
            Directory.CreateDirectory(DestPath);
        }

        // Если нет папки Logs - создаём.
        if (!Directory.Exists(LogsPath))
        {
            Directory.CreateDirectory(LogsPath);
        }

        // Наборы типов файлов с ключами.
        types.Add("Images", new List<string>() { "bmp", "ecw", "gif", "ico", "ilbm", "jpeg", "jpeg 2000", "Mrsid", "pcx", "png", "psd", "tga", "tiff", "jfif", "hd photo", "webp", "xbm", "xps", "rla", "rpf", "pnm" });
        types.Add("Music", new List<string>() { "3gp", "aa", "aac", "aax", "act", "aiff", "alac", "amr", "ape", "au", "awb", "dss", "dvf", "flac", "gsm", "iklax", "ivs", "m4a", "m4b", "m4p", "mmf", "mp3", "mpc", "msv", "nmf", "ogg", "oga", "mogg", "opus", "ra", "rm", "raw", "rf64", "sln", "tta", "voc", "wav", "wma", "wv", "webm", "8svx", "cda" });
        types.Add("Video", new List<string>() { "webm", "mkv", "flv", "vob", "ogv", "drc", "gif", "gifv", "mng", "avi", "mts", "m2ts", "ts", "mov", "qt", "wmv", "yuv", "rmvb", "viv", "asf", "amv", "mp4", "m4p", "m4v", "mpg", "mp2", "mpeg", "mpe", "mpv", "m2v", "m4v", "svi", "3gp", "3g2", "3g2", "roq", "msv", "flv", "f4v", "f4p", "f4a", "f4b" });
        types.Add("Document", new List<string>() { "asp", "cdd", "cpp", "doc", "docm", "docx", "dot", "dotm", "dotx", "epub", "fb2", "gpx", "ibooks", "indd", "kdc", "key", "kml", "mdb", "mdf", "mobi", "mso", "ods", "odt", "one", "oxps", "pages", "pdf", "pkg", "pl", "pot", "potm", "potx", "pps", "ppsm", "ppsx", "ppt", "pptx", "ps", "pub", "rtf", "sdf", "sgm1", "sldm", "snb", "wpd", "wps", "xar", "xlr", "xls", "xlsb", "xlsm", "slsx", "accdb", "xlt", "xltm", "xltx", "xps" });
        types.Add("Archive", new List<string>() { "a", "ar", "cpio", "shar", "lbr", "iso", "lbr", "mar", "sbx", "tar", "bz2", "f", "?xf", "gz", "lz", "lz4", "lzma", "lzo", "lz", "sfark", "sz", "?q?", "?z", "xz", "z", "zst", "??_", "7z", "s7z", "ace", "afa", "alz", "apk", "arc", "ark", "cdx", "arj", "b1", "b6z", "ba", "bh", "cab", "car", "cfs", "cpt", "dar", "dd", "dgc", "dmg", "ear", "gca", "genozip", "ha", "hki", "ice", "jar", "kgb", "lzh", "lha", "lzx", "pak", "partimg", "paq6", "paq7", "paq8", "pea", "phar", "pim", "pit", "qda", "rar", "rk", "sda", "sea", "sen", "sfx", "shk", "sit", "sitx", "sqx", "tar.gz", "tgz", "tar.z", "tar.bz2", "tbz2", "tar.lz", "tlz", "tar.xz", "txz", "tar.zst", "uc", "uc0", "uc2", "ucn", "ur2", "ue2", "uca", "uha", "war", "wim", "xar", "xp3", "yz1", "zip", "zipx", "zoo", "zpaq", "zz", "ecc", "ecsbx", "par", "par2", "rev" });
        types.Add("Executable", new List<string>() { "exe", "msi", "bat", "sh", "cmd", "com" });
        types.Add("Trash", new List<string>() { "torrent" });

        // Создание папок по ключам из словаря.
        foreach (string folder in types.Keys)
        {
            if (!Directory.Exists(DestPath + "\\" + folder))
            {
                Directory.CreateDirectory(DestPath + "\\" + folder);
            }
        }

        // Создание папки Other, если её нет. Служит для файлов, которые никуда не могут быть пересены.
        if (!Directory.Exists(DestPath + "\\Other"))
        {
            Directory.CreateDirectory(DestPath + "\\Other");
        }

        // Класс, который вызывает Update при появлении нового файла в очереди.
        FileSystemWatcher systemWatcher = new FileSystemWatcher(SourcePath);
        systemWatcher.EnableRaisingEvents = true;
        systemWatcher.Created += Update;
        systemWatcher.IncludeSubdirectories = false;

        // Заглушка метода TryPeek.
        string results;

        while (true)
        {
            // Ограничиваем потоки в 10 и проверяем не пуста ли директория.
            if (ActiveThreatingTasks < 10 && AllValues.TryPeek(out results))
            {
                // Запускаем потоки по перемещению следующего файла, который находится первым в очереди.
                Thread thread = new Thread(new ParameterizedThreadStart(SortFile));
                thread.Start(AllValues.Dequeue());
                // Увеличиваем число активных потоков.
                ActiveThreatingTasks++;
            }
        }
    }

    /// <summary>
    /// Метод сортировки файла.
    /// </summary>
    /// <param name="filePath">Полный путь к файлу</param>
    private static void SortFile(object filePath)
    {
        // Получаем информацию о файле по пути.
        FileInfo fileInfo = new FileInfo(Convert.ToString(filePath));

        // Заглушка перемещения внутрь папки Source.
        StreamReader streamReader;
        try
        {
            streamReader = new StreamReader(fileInfo.FullName);
        }
        catch
        {
            // Не допускаем переполнение стека.
            Thread.Sleep(1000);
            // Ожидаем, пока файл будет полностью перемещён в папку Source.
            SortFile(filePath);
            return;
        }
        

        // Позиция чтения файла.
        int count = 0;
        // Текущий символ.
        int charact;

        // Если есть такой лог, то удаляем.
        if (File.Exists(LogsPath + "\\" + fileInfo.Name + ".log"))
        {
            File.Delete(LogsPath + "\\" + fileInfo.Name + ".log");
        }

        // Пока файл не кончится.
        while (streamReader.Peek() >= 0)
        {
            // Читаем файл посимвольно.
            charact = streamReader.Read();

            // Увеличиваем позицию для записи в файл.
            count++;

            // Если встречаем \r
            if (charact == '\r')
            {
                // Читаем ещё символ
                charact = streamReader.Read();

                // Увеличиваем позицию для записи в файл.
                count++;

                // Если встречаем последовательность.
                if (charact == '\n')
                {
                    // Добавляем в файл позицию.
                    StreamWriter writer = new StreamWriter(LogsPath + "\\" + fileInfo.Name + ".log", true);
                    writer.Write(count.ToString() + ", ");
                    writer.Close();
                }
            }
        }

        // Выключаем чтение.
        streamReader.Close();

        // Сортировка
        foreach (KeyValuePair<string, List<string>> type in types)
        {
            foreach (string name in type.Value)
            {
                if ("." + name == fileInfo.Extension.ToLower())
                {
                    MoveFile(fileInfo, type.Key.ToString());
                    return;
                }
            }
        }

        // Если не отсортировался
        MoveFile(fileInfo, "Other");
    }

    private static void MoveFile(FileInfo fileInfo, string type)
    {
        try
        {
            string newName = Path.GetFileNameWithoutExtension(fileInfo.FullName);
            string newPath = DestPath + "\\" + type + "\\";

            if (File.Exists(newPath + fileInfo.Name))
            {
                newName = GetRenamedFile(fileInfo, newPath);
            }

            fileInfo.MoveTo(newPath + newName + fileInfo.Extension);
            ActiveThreatingTasks--;
            return;
        }
        catch (FileNotFoundException)
        {
            ActiveThreatingTasks--;
            return;
        }
        catch (IOException)
        {
            Thread.Sleep(1000);
            MoveFile(fileInfo, type);
        }
        catch
        {
            AllValues.Enqueue(fileInfo.FullName);
            ActiveThreatingTasks--;
            return;
        }
    }

    private static string GetRenamedFile(FileInfo fileInfo, string path)
    {
        string currentName;
        int filecount = 1;

        do
        {
            currentName = Path.GetFileNameWithoutExtension(fileInfo.FullName);
            currentName = currentName + " (" + filecount + ") ";
            filecount++;
        }
        while (File.Exists(path + currentName + fileInfo.Extension));

        return currentName;
    }
}
