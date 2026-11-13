// Copyright (c) 2026 Jonathan Belson
// Licensed under the MIT License — use freely, keep this notice.
// SPDX-License-Identifier: MIT
// Full terms: see LICENSE in the project root.

﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.IO.Compression;
//using System.IO.Compression.ZipFile;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
//using System.Windows.Shapes;

namespace RayTrace
{
    /// <summary>
    /// Interaction logic for SkyboxSelectDlg.xaml
    /// </summary>
    public partial class SkyboxSelectDlg : Window
    {
        public struct SkyboxEntry
        {
            public string Name { get; set; }
            public string Path { get; set; }
            public string File { get; set; }

            public string TempFile { get; set; }
        };

        private string _tempDir;

        public ObservableCollection<SkyboxEntry> Skyboxes { get; set; } = new();

        private object _lock = new();

        public SkyboxEntry Selection { get; set; } = new();

        ~SkyboxSelectDlg()
        {
            //Skyboxes.Clear();

            //skyboxList.ItemsSource = null;

            try
            {
                foreach (SkyboxEntry entry in Skyboxes)
                {
                    File.Delete(entry.TempFile);
                }
            }
            catch (Exception e)
            {
                Logger.Instance.Warn($"{e.Message}");
            }

            try
            {
                if (Directory.Exists(_tempDir))
                {
                    Directory.Delete(_tempDir, true);
                }
            }
            catch (Exception e)
            {
                Logger.Instance.Warn($"{e.Message}");
            }
        }

        //
        // Run ScanSkyboxes as a Task.
        //
        private async Task ScanSkyboxesAsync()
        {
            await Task.Run(() =>
            {
                ScanSkyboxes();
            });
        }

        //
        // Scan the 'Skybox' directory for valid skybox files.
        //
        private void ScanSkyboxes()
        {
            string[] files = Directory.GetFiles("Data/Skyboxes/", "*.zip");

            //string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            string path = _tempDir;

            foreach (string file in files)
            {
                using ZipArchive archive = ZipFile.OpenRead(file);

                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (entry.FullName.Equals("negz.jpg"))
                    {
                        string baseName = Path.GetFileNameWithoutExtension(file);

                        string filePath = System.IO.Path.Combine(path, baseName);

                        entry.ExtractToFile(filePath, true);

                        lock (_lock)
                        {
                            Skyboxes.Add(new SkyboxEntry()
                            {
                                Name = baseName,
                                Path = entry.FullName,
                                File = file,
                                TempFile = filePath,
                            });
                        }
                    }

                }

            }
        }

        //
        // Constructor.
        //
        public SkyboxSelectDlg()
        {
            InitializeComponent();

            DataContext = this;

            BindingOperations.EnableCollectionSynchronization(Skyboxes, _lock);

            try
            {
                _tempDir = Path.Combine(Storage.TempDir, Guid.NewGuid().ToString());
                Directory.CreateDirectory(_tempDir);
            }
            catch (Exception /*e*/)
            {
                _ = 0;
            }

            //ScanSkyboxes();

            _ = ScanSkyboxesAsync();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;

            //DeleteTempFiles();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;

            //DeleteTempFiles();
        }
    }
}
