using Dapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace OutsideWorldInteraction
{
	/// <summary>
    /// Класс для самообновления StationAgent
    /// </summary>
    public class StationAgentUpdater
    {
        private const string RESTARTER_SERVICE_NAME = "StationAgentUpdater.exe";

        private const string AGENT_NAME = "StationAgent";

        private const string BACKUP_FILES_SUBFOLDER = "Backup";
        private const string UPDATE_FILES_SUBFOLDER = "Update";
        private readonly ApiProxy _apiProxy;
        private readonly BaseRepositoryConfig _repositoryConfig;
        private readonly OutsideWorldInteractionWorkerConfig _outsideWorldConfig;
        private readonly ILogger _logger;

        public StationAgentUpdater(ApiSettings apiSettings, BaseRepositoryConfig repositoryConfig, OutsideWorldInteractionWorkerConfig outsideWorldConfig, ILogger logger)
        {
            _logger = logger;
            _repositoryConfig = repositoryConfig;
            _outsideWorldConfig = outsideWorldConfig;
            _apiProxy = new ApiProxy(apiSettings, logger);
        }

        /// <summary>
        /// Обновить StationAgent
        /// </summary>
        /// <returns>Удалось выполнить обновление или нет</returns>
        public async Task<bool> Update()
        {
            this.LogOnMethodEnter();

            var latestVersionName = await _apiProxy.GetLatestVersion();
            if (!string.IsNullOrEmpty(latestVersionName))
            {
                _logger.Info($"Текущая версия ПО {Assembly.GetExecutingAssembly().GetName().Version.ToString()}");
                UnzipFileToDisk(latestVersionName);
                if (TryUpdate())
                {
                    DeleteZipFile(latestVersionName);
                    _logger.Info($"ПО обновлено.");
                    await _apiProxy.SaveDeviceClientVersion(new DeviceClientVersion()
                    {
                        Id = Guid.NewGuid(),
                        ManagedObjectId = _outsideWorldConfig.Telemetry.Id,
                        UpdateDate = DateTime.Now
                    });
                    KillApplication();

                    return this.LogOnReturn(true);
                }
                _logger.Info("При обновлении StationAgent возникла ошибка. Файлы с новой версией не подменены на рабочие. Приложение не перезапущено. ");
            }
            _logger.Info("При обновлении StationAgent возникла ошибка. Не получен файл с новой версией. ");

            return this.LogOnReturn(false);
        }

        /// <summary>
        /// Заменяет имеющиеся dll новыми версиями. Eсли возникла ошибка, делается откат
        /// </summary>
        /// <returns>Успешно ли прошла подмена dll</returns>
        private bool TryUpdate()
        {
            this.LogOnMethodEnter();

            var result = true;
            if (AppDomain.CurrentDomain.IsDefaultAppDomain())
            {
                // Get the startup path.
                string assemblyPath = Assembly.GetExecutingAssembly().Location;
                string assemblyDirectory = Path.GetDirectoryName(assemblyPath);

                // Check deleted files folders existance
                string backupDirectory = Path.Combine(assemblyDirectory, BACKUP_FILES_SUBFOLDER);
                string updateDirectory = Path.Combine(assemblyDirectory, UPDATE_FILES_SUBFOLDER);

                CreateCleanDirectory(backupDirectory);

                var updateFilesList = new List<string>(); // Коллекция с теми файлами, которые мы обновили в директории программы
                UpdateSettingsObject updateAppSettingsScript = null;

                try
                {
                    if (Directory.Exists(updateDirectory))
                    {
                        foreach (string newFile in Directory.GetFiles(updateDirectory, "*", SearchOption.AllDirectories))
                        {
                            if (CheckUpdateAppSettingsScript(newFile, ref updateAppSettingsScript))
                            {
                                continue;
                            }
                            //если есть файл sql, то надо обновить БД
                            if(Path.GetExtension(newFile).Equals(".sql"))
                            {
                                UpdateDatabase(newFile);
                                continue;
                            }

                            var fileName = Path.GetFileName(newFile);

                            if (fileName == AGENT_NAME)
                            {
                                _logger.Info($"Пропускаем файл {fileName}");
                                continue;
                            }

                            string backupingFile = Path.Combine(assemblyDirectory, fileName);

                            if (File.Exists(backupingFile)) // Забэкапим обновляемый файл, если он есть в директории программы
                            {
                                string backupedFile = Path.Combine(assemblyDirectory, BACKUP_FILES_SUBFOLDER, fileName);
                                CreateDirectoryIfNotExists(backupedFile);
                                _logger.Info($"Делаем бекап файла {fileName}");
                                File.Move(backupingFile, backupedFile);
                            }

                            // Скопируем новый файл и создадим структуру папок для него, если необходимо
                            CreateDirectoryIfNotExists(backupingFile);
                            _logger.Info($"Копируем файл {fileName}");
                            File.Copy(newFile, backupingFile, true);

                            updateFilesList.Add(backupingFile);
                        }

                        UpdateAppSettings(updateAppSettingsScript);
                    }
                }
                catch (Exception ex)
                {
                    // Обновить не получилось. Будем откатываться
                    result = false;

                    // Удалим добавленные файлы
                    foreach (var file in updateFilesList)
                    {
                        File.Delete(file);
                    }

                    // Восстановим файлы из бэкапа
                    foreach (string file in Directory.GetFiles(backupDirectory, "*", SearchOption.AllDirectories))
                    {
                        var fileName = Path.GetFileName(file);

                        var backupFile = Path.Combine(assemblyDirectory, fileName);
                        CreateDirectoryIfNotExists(backupFile);
                        _logger.Info($"Возвращаем файл {fileName}");
                        File.Copy(file, backupFile, true);
                    }
                    _logger.Error(ErrorProcessing.ExceptionMessageToString(ex));
                }
            }

            return this.LogOnReturn(result);
        }

        /// <summary>
        /// Распаковывает архив с последней версией на диск
        /// </summary>
        /// <param name="path">Путь до версии</param>
        public void UnzipFileToDisk(string path)
        {
            this.LogOnMethodEnter();

            string assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string assemblyDirectory = Path.GetDirectoryName(assemblyPath);
            string deletionDirectory = Path.Combine(assemblyDirectory, UPDATE_FILES_SUBFOLDER);

            if (AppDomain.CurrentDomain.IsDefaultAppDomain())
            {
                _logger.Info("Создаем каталог для обновленных файлов...");
                CreateCleanDirectory(deletionDirectory);
            }

            _logger.Info("Извелкаем обновленную версию...");
            ZipFile.ExtractToDirectory(Path.Combine(assemblyDirectory, path), deletionDirectory, true);

            this.LogOnMethodExit();
        }

		/// <summary>
        /// Удаляет архив с версией 
        /// </summary>
        /// <param name="latestVersionName">Номер последней версии</param>
        private void DeleteZipFile(string latestVersionName)
        {
            this.LogOnMethodEnter();

            string assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string assemblyDirectory = Path.GetDirectoryName(assemblyPath);

            var path = Path.Combine(assemblyDirectory, latestVersionName);
            if (AppDomain.CurrentDomain.IsDefaultAppDomain())
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }

            this.LogOnMethodExit();
        }

        /// <summary>
        /// Создает папку, если такая уже есть, то удаляет и создает пустую
        /// </summary>
        /// <param name="path">путь до дирректории, которую нужно очистить или создать</param>
        private void CreateCleanDirectory(string path)
        {
            this.LogOnMethodEnter();

            var di = new DirectoryInfo(path);
            if (di.Exists)
            {
                di.Delete(true);
            }

            Directory.CreateDirectory(path);

            this.LogOnMethodExit();
        }

        /// <summary>
        /// Создает структуру папок для файла, если необходимо
        /// </summary>
        /// <param name="filePath">Путь до папки, которую нужно создать</param>
        private void CreateDirectoryIfNotExists(string filePath)
        {
            this.LogOnMethodEnter();

            var dirWithNewFile = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dirWithNewFile))
            {
                Directory.CreateDirectory(dirWithNewFile);
            }

            this.LogOnMethodExit();
        }

        /// <summary>
        /// Обновление БД новой версией программы
        /// </summary>
        /// <param name="fileName">Имя файла</param>
        /// <returns>Удалось ли обновить версию в БД</returns>
        private bool UpdateDatabase(string fileName)
        {
            var conn = new NpgsqlConnection(_repositoryConfig.ConnectionString);

            string sqlScript = File.ReadAllText(fileName);

            var result = conn.Execute(sqlScript);

            return result >= 0 ? true : false; 
        }

		/// <summary>
        /// Закрывает приложение
        /// </summary>
        private void KillApplication()
        {
            this.LogOnMethodEnter();

            var proc = new Process();
            Environment.Exit(0);

            this.LogOnMethodExit();
        }
        
		/// <summary>
        /// Десериализация скрипта файла настроек
        /// </summary>
        /// <param name="path">Имя файла</param>
        /// <param name="script">Выходной текст скрипта настроек</param>
        /// <returns>Удалась десериализация или нет</returns>
        private bool CheckUpdateAppSettingsScript(string path, ref UpdateSettingsObject script)
        {
            var fileName = Path.GetFileName(path);
            if (!"updateAppSettings.script".Equals(fileName))
            {
                return false;
            }

            try
            {
                var jsonText = File.ReadAllText(path);
                script = JsonConvert.DeserializeObject<UpdateSettingsObject>(jsonText);
                return true;
            }
            catch
            {
                _logger.Error($"Не удалось прочитать скрипт обновления файла настроек");
                return false;
            }
        }
        
		/// <summary>
        /// Обновление скрипта файла настроек
        /// </summary>
        /// <param name="updateAppSettingsScript">текст скрипта</param>
        private void UpdateAppSettings(UpdateSettingsObject updateAppSettingsScript)
        {
            if (updateAppSettingsScript == null)
            {
                return;
            }

            try
            {
                _logger.Info($"Обновляем файл настроек...");

                string assemblyPath = Assembly.GetExecutingAssembly().Location;
                string assemblyDirectory = Path.GetDirectoryName(assemblyPath);

                string appSettingsPath = Path.Combine(assemblyDirectory, "appsettings.json");
                var currentSettingsText = File.ReadAllText(appSettingsPath);
                var currentSettings = JObject.Parse(currentSettingsText).SelectToken("");

                if (updateAppSettingsScript.Remove?.Any() ?? false)
                {
                    foreach (var item in updateAppSettingsScript.Remove)
                    {
                        try
                        {
                            var path = item.Split(".");
                            var currentToken = currentSettings;
                            for (int i = 0; i < path.Length - 1; i++)
                            {
                                currentToken = currentToken?.SelectToken(path[i]);
                            }
                            (currentToken as JObject)?.Property(path.Last())?.Remove();

                            _logger.Info($"Successfully removed {item}");
                        }
                        catch (Exception ex)
                        {
                            _logger.Error($"Cannot remove {item}\r\n{ex.Message}");
                        }
                    }
                }
                if (updateAppSettingsScript.Add?.Any() ?? false)
                {
                    foreach (var item in updateAppSettingsScript.Add)
                    {
                        try
                        {
                            var path = item.Key.Split(".");
                            var currentToken = currentSettings;
                            for (int i = 0; i < path.Length - 1; i++)
                            {
                                currentToken = currentToken?.SelectToken(path[i]);
                            }
                            (currentToken as JObject)?.Add(path.Last(), new JValue(item.Value));

                            _logger.Info($"Successfully added {item.Key}");
                        }
                        catch (Exception ex)
                        {
                            _logger.Error($"Cannot add {item.Key}\r\n{ex.Message}");
                        }
                    }
                }
                if (updateAppSettingsScript.Set?.Any() ?? false)
                {
                    foreach (var item in updateAppSettingsScript.Set)
                    {
                        try
                        {
                            var path = item.Key.Split(".");
                            var currentToken = currentSettings;
                            for (int i = 0; i < path.Length - 1; i++)
                            {
                                currentToken = currentToken?.SelectToken(path[i]);
                            }
                            (currentToken as JObject)?.Property(path.Last())?.Replace(new JValue(item.Value));

                            _logger.Info($"Successfully changed {item.Key}");
                        }
                        catch (Exception ex)
                        {
                            _logger.Error($"Cannot change {item.Key}\r\n{ex.Message}");
                        }
                    }
                }

                var newValue = currentSettings.ToString();
                Console.WriteLine(newValue);
                File.WriteAllText("new.json", newValue);
                Console.SetCursorPosition(0, 0);
                Console.ReadKey();

                _logger.Info($"Файл настроек успешно обновлен");
            }
            catch
            {
                _logger.Error($"Не удалось выполнить скрипт обновления файла настроек");
            }
        }
    }
}
