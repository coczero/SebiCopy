﻿using Caliburn.Micro;
using Microsoft.VisualBasic.FileIO;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using System.Windows.Threading;
using xZune.Vlc.Wpf;

namespace SenpaiCopy
{
	/// <summary>
	/// ViewModel for the MainView.
	/// </summary>
	internal class MainViewModel : PropertyChangedBase
	{
		#region Private Member

		/// <summary>
		/// The index of the current image of the <see cref="ImagePathList"/>
		/// </summary>
		private int _currentImageIndex;

		/// <summary>
		/// The last right clicked CheckBox.
		/// </summary>
		private PathCheckBox _currentRightClickedCheckBox;

		/// <summary>
		/// BackgroundWorker used to do the google reverse image search.
		/// </summary>
		private BackgroundWorker _reverseImageSearchWorker;

		/// <summary>
		/// Current dispatcher. Used in the reverse image search BackgroundWorker.
		/// </summary>
		private Dispatcher _dispatcher;

		/// <summary>
		/// Number of images in this session.
		/// A new session gets created when
		/// a new image folder is selected.
		/// </summary>
		private double _sessionCount;

		#endregion Private Member

		#region Properties

		/// <summary>
		/// Gets/sets the <see cref="SettingsViewModel"/> for this ViewModel.
		/// </summary>
		public SettingsViewModel SettingsViewModel
		{
			get { return _settingsViewModel; }
			private set
			{
				_settingsViewModel = value;
				NotifyOfPropertyChange(() => SettingsViewModel);
			}
		}
		private SettingsViewModel _settingsViewModel;

		/// <summary>
		/// Gets/sets the list of image paths.
		/// </summary>
		public ObservableCollection<FileInfo> ImagePathList
		{
			get
			{
				if (ImagePathFilter != "")
					return new ObservableCollection<FileInfo>(_imagePathList.Where(i => i.Name.ToLower().Contains(ImagePathFilter.ToLower())).ToList());
				else
					return _imagePathList;
			}
		}
		private ObservableCollection<FileInfo> _imagePathList;

		/// <summary>
		/// Gets/sets the string for filtering images.
		/// </summary>
		public string ImagePathFilter
		{
			get { return _imagePathFilter; }
			set
			{
				_imagePathFilter = value;
				NotifyOfPropertyChange(() => ImagePathList);
				NotifyOfPropertyChange(() => ImagePathFilter);
			}
		}
		private string _imagePathFilter;

		/// <summary>
		/// Gets/sets the currently shown image.
		/// </summary>
		public ImageSource CurrentImage
		{
			get { return _currentImage; }
			private set
			{
				_currentImage = value;
				NotifyOfPropertyChange(() => CurrentImage);
				NotifyImagePropertyChanges();
			}
		}
		private ImageSource _currentImage;

		/// <summary>
		/// Gets a list of all PathCheckBoxes.
		/// </summary>
		public ObservableCollection<PathCheckBox> CheckBoxList
		{
			get
			{
				if (CheckBoxFilter != "")
					return new ObservableCollection<PathCheckBox>(_checkBoxList.Where(i => (i.Content as TextBlock).Text.ToLower().Contains(CheckBoxFilter.ToLower())).ToList());
				else
					return _checkBoxList;
			}
		}
		private ObservableCollection<PathCheckBox> _checkBoxList;

		/// <summary>
		/// Gets/sets the string for filtering paths.
		/// </summary>
		public string CheckBoxFilter
		{
			get { return _checkBoxFilter; }
			set
			{
				_checkBoxFilter = value;
				NotifyOfPropertyChange(() => CheckBoxList);
				NotifyOfPropertyChange(() => CheckBoxFilter);
			}
		}
		private string _checkBoxFilter;

		/// <summary>
		/// Gets/sets wether subdirectories get included in select image path.
		/// </summary>
		public bool IncludeImageSubDirectories
		{
			get { return _includeImageSubDirectories; }
			set { _includeImageSubDirectories = value; }
		}
		private bool _includeImageSubDirectories;

		/// <summary>
		/// Gets/sets wether subdirectories get included in select folder path.
		/// </summary>
		public bool IncludeFolderSubDirectories
		{
			get { return _includeFolderSubDirectories; }
			set { _includeFolderSubDirectories = value; }
		}
		private bool _includeFolderSubDirectories;

		/// <summary>
		/// Gets/sets the path to the folder with the images.
		/// </summary>
		public string ImagePath
		{
			get { return _imagePath; }
			private set
			{
				_imagePath = value;
				NotifyOfPropertyChange(() => ImagePath);
			}
		}
		private string _imagePath;

		/// <summary>
		/// Gets/sets the path to the folder of the folders.
		/// </summary>
		public string FolderPath
		{
			get { return _folderPath; }
			private set
			{
				_folderPath = value;
				NotifyOfPropertyChange(() => FolderPath);
				NotifyOfPropertyChange(() => CanAddFolder);
			}
		}
		private string _folderPath;

		/// <summary>
		/// Gets/sets wether the image should be deleted after copying.
		/// </summary>
		public bool DeleteImage
		{
			get { return _deleteImage; }
			set
			{
				_deleteImage = value;
				NotifyOfPropertyChange(() => CanCopy);
				NotifyOfPropertyChange(() => ExecuteButtonColor);
			}
		}
		private bool _deleteImage;

		/// <summary>
		/// Gets/sets wether the path checkBoxes should be reset after executing.
		/// </summary>
		public bool ResetCheckBoxes
		{
			get { return _resetCheckBoxes; }
			set { _resetCheckBoxes = value; }
		}
		private bool _resetCheckBoxes;

		/// <summary>
		/// Gets/sets the ignored paths.
		/// </summary>
		public ObservableCollection<string> IgnoredPaths
		{
			get { return _ignoredPaths; }
			private set { _ignoredPaths = value; }
		}
		private ObservableCollection<string> _ignoredPaths;

		/// <summary>
		/// Gets/sets the favorite paths.
		/// </summary>
		public ObservableCollection<string> FavoritePaths
		{
			get { return _favoritePaths; }
			private set { _favoritePaths = value; }
		}
		private ObservableCollection<string> _favoritePaths;

		/// <summary>
		/// Gets/sets the currently selected image in the list.
		/// </summary>
		public FileInfo SelectedImage
		{
			get { return _selectedImage; }
			set
			{
				_selectedImage = value;
				if (value != null)
				{
					_currentImageIndex = _imagePathList.IndexOf(SelectedImage);
					UpdatePictureBox();
				}
			}
		}
		private FileInfo _selectedImage;

		/// <summary>
		/// Gets/sets the command for pressed hotkeys.
		/// </summary>
		public ICommand HotkeyPressedCommand
		{
			get { return _hotkeyPressedCommand; }
			private set { _hotkeyPressedCommand = value; }
		}
		private ICommand _hotkeyPressedCommand;

		/// <summary>
		/// Progress bar in the TaskBar for showing session progress.
		/// </summary>
		public TaskbarItemInfo TaskbarProgress
		{
			get { return _taskbarProgress; }
			private set
			{
				_taskbarProgress = value;
				NotifyOfPropertyChange(() => TaskbarProgress);
			}
		}
		private TaskbarItemInfo _taskbarProgress;

		/// <summary>
		/// Gets/sets the vlc player control.
		/// </summary>
		public VlcPlayer VlcPlayer
		{
			get { return _vlcPlayer; }
			private set
			{
				_vlcPlayer = value;
				NotifyOfPropertyChange(() => VlcPlayer);
			}
		}
		private VlcPlayer _vlcPlayer;

		/// <summary>
		/// The current image of the reverse image search button.
		/// </summary>
		public ImageSource ReverseImageSearchButtonImage
		{
			get { return _reverseImageSearchButtonImage; }
			private set
			{
				_reverseImageSearchButtonImage = value;
				NotifyOfPropertyChange(() => ReverseImageSearchButtonImage);
			}
		}
		private ImageSource _reverseImageSearchButtonImage;

		/// <summary>
		/// Gets the text of the favorite context menu item.
		/// </summary>
		public string FavoriteContextMenuItemText
		{
			get
			{
				if ((_currentRightClickedCheckBox.Content as TextBlock).Background == null)
					return "Add to favorites";
				else
					return "Remove from favorites";
			}
		}

		/// <summary>
		/// Gets the image of the favorite context menu item.
		/// </summary>
		public ImageSource FavoriteContextMenuItemImage
		{
			get
			{
				if ((_currentRightClickedCheckBox.Content as TextBlock).Background == null)
					return new BitmapImage(new Uri("pack://application:,,,/SenpaiCopy;component/Resources/favAdd.png"));
				else
					return new BitmapImage(new Uri("pack://application:,,,/SenpaiCopy;component/Resources/favRemove.png"));
			}
		}

		/// <summary>
		/// Gets if the previous button is enabled.
		/// </summary>
		public bool CanPrevious
		{
			get { return _currentImageIndex != 0; }
		}

		/// <summary>
		/// Gets if the copy button is enabled.
		/// </summary>
		public bool CanCopy
		{
			get { return ImageLoaded && (DeleteImage || _checkBoxList.Count(i => (bool)i.IsChecked) != 0); }
		}

		/// <summary>
		/// Gets if the next button is enabled.
		/// </summary>
		public bool CanNext
		{
			get { return _imagePathList.Count - 1 > _currentImageIndex; }
		}

		/// <summary>
		/// Gets wether the user can click the "Add Folder" button in
		/// the context menu of the <see cref="FolderPath"/> label.
		/// </summary>
		public bool CanAddFolder
		{
			get { return FolderPath != null && FolderPath != ""; }
		}

		/// <summary>
		/// Gets if the current image can be searched with the google reverse image search.
		/// </summary>
		public bool CanReverseImageSearch
		{
			get { return CurrentImage != null && ImageFileSize <= 15; }
		}

		/// <summary>
		/// Gets the color for the execute button.
		/// </summary>
		public SolidColorBrush ExecuteButtonColor
		{
			get
			{
				if (!CanCopy)
					return new SolidColorBrush(Colors.Gray);
				if (DeleteImage && _checkBoxList.Count(i => (bool)i.IsChecked) == 0)
					return new SolidColorBrush(Colors.Red);
				else
					return new SolidColorBrush(Colors.Green);
			}
		}

		/// <summary>
		/// Gets if the VlcPlayer should be visible on the UI.
		/// </summary>
		public bool VlcPlayerVisible
		{
			get
			{
				if (_imagePathList.Count == 0)
					return false;
				else
					return _imagePathList[_currentImageIndex].Extension == ".webm";
			}
		}

		/// <summary>
		/// Gets if an image is loaded.
		/// </summary>
		public bool ImageLoaded
		{
			get
			{
				if (CurrentImage != null)
					return true;
				else
				{
					if (_imagePathList.Count == 0)
						return false;
					else if (_imagePathList.Count == _currentImageIndex)
						return false;
					else
						return _imagePathList[_currentImageIndex].Extension == ".webm";
				}
			}
		}

		/// <summary>
		/// Gets the width of the current image.
		/// </summary>
		public int ImageWidth
		{
			get
			{
				if (ImageLoaded)
				{
					if (_imagePathList[_currentImageIndex].Extension == ".webm")
						return (int)VlcPlayer.VlcMediaPlayer.VideoSize.Width;
					else
						return (int)CurrentImage.Width;
				}

				return 0;
			}
		}

		/// <summary>
		/// Gets the height of the current image.
		/// </summary>
		public int ImageHeight
		{
			get
			{
				if (ImageLoaded)
				{
					if (_imagePathList[_currentImageIndex].Extension == ".webm")
						return (int)VlcPlayer.VlcMediaPlayer.VideoSize.Height;
					else
						return (int)CurrentImage.Height;
				}

				return 0;
			}
		}

		/// <summary>
		/// Gets the name of the current file.
		/// </summary>
		public string ImageFileName
		{
			get
			{
				if (ImageLoaded)
					return _imagePathList[_currentImageIndex].FullName;
				else
					return "";
			}
		}

		/// <summary>
		/// Gets the size of the current image in megabytes.
		/// </summary>
		public double ImageFileSize
		{
			get
			{
				if (ImageLoaded)
					return Math.Round(_imagePathList[_currentImageIndex].Length / 1024.0 / 1024.0, 2);
				else
					return 0.0;
			}
		}

		#endregion Properties

		/// <summary>
		/// Ctor.
		/// </summary>
		public MainViewModel()
		{
			SettingsViewModel = new SettingsViewModel();
			_checkBoxFilter = "";
			_imagePathFilter = "";
			_imagePathList = new ObservableCollection<FileInfo>();
			_checkBoxList = new ObservableCollection<PathCheckBox>();
			IncludeFolderSubDirectories = true;
			DeleteImage = true;
			ResetCheckBoxes = true;
			LoadIgnoredPaths();
			LoadFavoritePaths();
			HotkeyPressedCommand = new KeyCommand(HotkeyPressed);
			TaskbarProgress = new TaskbarItemInfo() { ProgressState = TaskbarItemProgressState.Normal };
			VlcPlayer = new VlcPlayer();
			ReverseImageSearchButtonImage = new BitmapImage(new Uri("pack://application:,,,/SenpaiCopy;component/Resources/google-favicon.png"));
			_reverseImageSearchWorker = new BackgroundWorker();
			_reverseImageSearchWorker.DoWork += new DoWorkEventHandler(GoogleReverseImageSearch);
			_dispatcher = Dispatcher.CurrentDispatcher;
		}

		/// <summary>
		/// Loads the ignored paths from the text file.
		/// </summary>
		private void LoadIgnoredPaths()
		{
			IgnoredPaths = new ObservableCollection<string>();

			if (File.Exists("IgnoredPaths.txt"))
			{
				string[] paths = File.ReadAllLines("IgnoredPaths.txt");
				foreach (string path in paths)
				{
					IgnoredPaths.Add(path);
				}
			}
			else
				File.Create("IgnoredPaths.txt");
		}

		/// <summary>
		/// Loads the favorite paths from the text file.
		/// </summary>
		private void LoadFavoritePaths()
		{
			FavoritePaths = new ObservableCollection<string>();

			if (File.Exists("FavoritePaths.txt"))
			{
				string[] paths = File.ReadAllLines("FavoritePaths.txt");
				foreach (string path in paths)
				{
					FavoritePaths.Add(path);
				}
			}
			else
				File.Create("FavoritePaths.txt");
		}

		/// <summary>
		/// Lets the user select a folder with images and loads them.
		/// </summary>
		public void SelectImageFolder()
		{
			FolderBrowserDialog dlg = new FolderBrowserDialog();
			dlg.SelectedPath = Properties.Settings.Default.LastSelectedImagePath;
			if (dlg.ShowDialog() == DialogResult.OK)
			{
				ImagePath = dlg.SelectedPath;
				_imagePathList.Clear();

				System.IO.SearchOption so;
				if (IncludeImageSubDirectories)
					so = System.IO.SearchOption.AllDirectories;
				else
					so = System.IO.SearchOption.TopDirectoryOnly;

				//get all files
				string[] files = Directory.GetFiles(dlg.SelectedPath, "*", so);

				//get all image files
				foreach (string file in files)
				{
					string lowerFile = file.ToLower();
					if (SettingsViewModel.EnabledFormats.Any(i => lowerFile.EndsWith(i)))
						_imagePathList.Add(new FileInfo(file));
				}

				TaskbarProgress.ProgressValue = 0.0;
				_sessionCount = _imagePathList.Count;

				Properties.Settings.Default.LastSelectedImagePath = dlg.SelectedPath;
				Properties.Settings.Default.Save();
			}

			UpdatePictureBox();
		}

		/// <summary>
		/// Opens a dialog to select a subfolder and creates CheckBoxes for each path.
		/// </summary>
		public void SelectFolderPath()
		{
			FolderBrowserDialog dlg = new FolderBrowserDialog();
			dlg.SelectedPath = Properties.Settings.Default.LastSelectedFolderPath;
			if (dlg.ShowDialog() == DialogResult.OK)
			{
				_checkBoxList.Clear();

				FolderPath = dlg.SelectedPath;

				System.IO.SearchOption so;
				if (IncludeFolderSubDirectories)
					so = System.IO.SearchOption.AllDirectories;
				else
					so = System.IO.SearchOption.TopDirectoryOnly;

				string[] subfolders = Directory.GetDirectories(dlg.SelectedPath, "*", so);

				List<PathCheckBox> _checkBoxes = new List<PathCheckBox>();
				foreach (string folder in subfolders)
				{
					AddCheckBox(folder);
				}

				Properties.Settings.Default.LastSelectedFolderPath = dlg.SelectedPath;
				Properties.Settings.Default.Save();
				NotifyOfPropertyChange(() => ExecuteButtonColor);
			}
		}

		/// <summary>
		/// Adds a new CheckBox with the given <paramref name="folder"/> to the list.
		/// </summary>
		/// <param name="folder">Folder to add.</param>
		private void AddCheckBox(string folder)
		{
			if (!IgnoredPaths.Contains(folder))
			{
				PathCheckBox chk = new PathCheckBox();
				chk.Content = new TextBlock() { Text = new DirectoryInfo(folder).Name };
				chk.FullPath = folder;
				chk.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
				chk.VerticalAlignment = VerticalAlignment.Stretch;
				chk.Margin = new Thickness(10, 0, 0, 0);
				chk.MouseRightButtonDown += new MouseButtonEventHandler(CheckBox_RightMouseDown);
				chk.Checked += CheckBox_CheckedChanged;
				chk.Unchecked += CheckBox_CheckedChanged;
				chk.ToolTip = folder;

				if (FavoritePaths.Contains(folder))
					(chk.Content as TextBlock).Background = new SolidColorBrush(Colors.Yellow);

				_checkBoxList.Add(chk);
			}
		}

		/// <summary>
		/// Triggers when a CheckBox gets checked or unchecked.
		/// Checks if the execute button needs a different color.
		/// </summary>
		private void CheckBox_CheckedChanged(object sender, RoutedEventArgs e)
		{
			NotifyOfPropertyChange(() => ExecuteButtonColor);
		}

		/// <summary>
		/// Updates the currently shown image.
		/// </summary>
		private void UpdatePictureBox()
		{
			if (_imagePathList.Count > _currentImageIndex)
			{
				try
				{
					if (_imagePathList[_currentImageIndex].Extension == ".webm")
					{
						VlcPlayer.BeginStop(ar =>
						{
							VlcPlayer.LoadMedia(_imagePathList[_currentImageIndex].FullName);
							VlcPlayer.Play();
							CurrentImage = null;
						});
					}
					else
					{
						CurrentImage = LoadBitmapImage(_imagePathList[_currentImageIndex].FullName);
					}
				}
				catch (Exception ex)
				{
					HandleError(ex);
				}
				finally
				{
					TaskbarProgress.ProgressValue = (_sessionCount - _imagePathList.Count) * 1.0 / _sessionCount;
				}
			}
			else // no more images.
			{
				CurrentImage = null;
				_currentImageIndex = 0;

				if (_imagePathList.Count != 0)
					UpdatePictureBox();
				else
					TaskbarProgress.ProgressValue = 1.0;
			}

			NotifyOfPropertyChange(() => CanPrevious);
			NotifyOfPropertyChange(() => CanNext);
			NotifyOfPropertyChange(() => VlcPlayerVisible);
		}

		/// <summary>
		/// Copies the current image to the selected folders.
		/// </summary>
		public void Execute()
		{
			List<string> dirsToCopyTo = new List<string>();

			foreach (PathCheckBox c in _checkBoxList)
			{
				if ((bool)c.IsChecked)
					dirsToCopyTo.Add(c.FullPath);
			}

			foreach (string dir in dirsToCopyTo)
			{
				try
				{
					_imagePathList[_currentImageIndex].CopyTo(dir + @"\" + _imagePathList[_currentImageIndex].Name, true);
				}
				catch (Exception ex)
				{
					HandleError(ex);
				}
			}

			if (DeleteImage)
			{
				try
				{
					VlcPlayer.BeginStop(ar =>
					{
						// do nothing. hacky
					});

					if(VlcPlayer.State != xZune.Vlc.Interop.Media.MediaState.Stopped)
					{
						VlcPlayer.BeginStop(ar =>
						{
							// do nothing. hacky
						});
					}

					FileSystem.DeleteFile(_imagePathList[_currentImageIndex].FullName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
					_imagePathList.RemoveAt(_currentImageIndex);
				}
				catch (Exception ex)
				{
					HandleError(ex);
				}
			}
			else
				_currentImageIndex++;

			if (ResetCheckBoxes)
				ClearCheckBoxes();

			UpdatePictureBox();
		}

		/// <summary>
		/// Unchecks all checked PathCheckBoxes.
		/// </summary>
		private void ClearCheckBoxes()
		{
			foreach (PathCheckBox c in _checkBoxList)
			{
				c.IsChecked = false;
			}
		}

		/// <summary>
		/// Skips the current image doing nothing with it.
		/// </summary>
		public void Next()
		{
			_currentImageIndex++;
			UpdatePictureBox();
		}

		/// <summary>
		/// Goes to the previous image.
		/// </summary>
		public void Previous()
		{
			_currentImageIndex--;
			UpdatePictureBox();
		}

		/// <summary>
		/// Opens the current image in the standard image viewer.
		/// </summary>
		public void ShowImage()
		{
			try
			{
				Process.Start((CurrentImage as BitmapImage).UriSource.LocalPath);
			}
			catch(Exception ex)
			{
				HandleError(ex);
			}
		}

		/// <summary>
		/// Removes the selected CheckBox.
		/// </summary>
		public void RemoveCheckBox()
		{
			_checkBoxList.Remove(_currentRightClickedCheckBox);
		}

		/// <summary>
		/// Adds the right clicked checkBox path to the <see cref="IgnoredPaths"/>
		/// </summary>
		public void IgnorePath()
		{
			File.AppendAllText("IgnoredPaths.txt", _currentRightClickedCheckBox.FullPath + "\r\n");
			IgnoredPaths.Add(_currentRightClickedCheckBox.FullPath);
			RemoveCheckBox();
		}

		/// <summary>
		/// Adds/removes the right clicked CheckBox to/from the favorites.
		/// </summary>
		public void AddPathToFavorites()
		{
			if ((_currentRightClickedCheckBox.Content as TextBlock).Background == null)
			{
				File.AppendAllText("FavoritePaths.txt", _currentRightClickedCheckBox.FullPath + "\r\n");
				FavoritePaths.Add(_currentRightClickedCheckBox.FullPath);
				(_currentRightClickedCheckBox.Content as TextBlock).Background = new SolidColorBrush(Colors.Yellow);
			}
			else
			{
				List<string> paths = File.ReadAllLines("FavoritePaths.txt").ToList();
				paths.Remove(_currentRightClickedCheckBox.FullPath);
				File.WriteAllLines("FavoritePaths.txt", paths.ToArray());
				(_currentRightClickedCheckBox.Content as TextBlock).Background = null;
			}
		}

		/// <summary>
		/// Shows a dialog to select a folder to add to the list.
		/// </summary>
		public void AddFolder()
		{
			AddFolder(_currentRightClickedCheckBox.FullPath);
		}

		/// <summary>
		/// Shows a dialog to select a folder to add to the list,
		/// setting the dialog to the <paramref name="dialogStartingPath"/> by default.
		/// </summary>
		/// <param name="dialogStartingPath">Default selected path of the FolderBrowserDialog.</param>
		public void AddFolder(string dialogStartingPath)
		{
			FolderBrowserDialog dlg = new FolderBrowserDialog();
			dlg.SelectedPath = dialogStartingPath;
			if (dlg.ShowDialog() == DialogResult.OK)
			{
				if (!_checkBoxList.Any(i => i.FullPath == dlg.SelectedPath))
					AddCheckBox(dlg.SelectedPath);
				else
					System.Windows.MessageBox.Show("This folder has already been added!");
			}
		}

		/// <summary>
		/// Loads an image file from the given <paramref name="fileName"/>.
		/// This does still allow operations done to the file.
		/// </summary>
		/// <param name="fileName">Image file to load.</param>
		/// <returns>Loaded image file.</returns>
		private static BitmapImage LoadBitmapImage(string fileName)
		{
			var bitmapImage = new BitmapImage();
			bitmapImage.BeginInit();
			bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
			bitmapImage.UriSource = new Uri(fileName);
			bitmapImage.EndInit();
			bitmapImage.Freeze();
			return bitmapImage;
		}

		/// <summary>
		/// Sets the current right clicked CheckBox.
		/// </summary>
		private void CheckBox_RightMouseDown(object sender, MouseButtonEventArgs e)
		{
			_currentRightClickedCheckBox = sender as PathCheckBox;
			NotifyOfPropertyChange(() => FavoriteContextMenuItemText);
			NotifyOfPropertyChange(() => FavoriteContextMenuItemImage);
		}

		/// <summary>
		/// Opens the right clicked folder in the explorer.
		/// </summary>
		public void ShowPathInExplorer()
		{
			try
			{
				Process.Start(_currentRightClickedCheckBox.FullPath);
			}
			catch(Exception)
			{
				System.Windows.MessageBox.Show("Error opening the folder. CheckBox will be removed.");
				RemoveCheckBox();
			}
		}

		public void OpenSettingsWindow()
		{
			SettingsView sv = new SettingsView();
			sv.DataContext = SettingsViewModel;
			sv.ShowDialog();
		}

		/// <summary>
		/// Executes the action associated with the pressed hotkey.
		/// </summary>
		/// <param name="parameter">Pressed key.</param>
		public void HotkeyPressed(object parameter)
		{
			Key pressedKey = (Key)parameter;

			if (pressedKey == SettingsViewModel.PreviousHotkey && CanPrevious)
				Previous();
			else if (pressedKey == SettingsViewModel.ExecuteHotkey && CanCopy)
				Execute();
			else if (pressedKey == SettingsViewModel.NextHotkey && CanNext)
				Next();
			else if (pressedKey == SettingsViewModel.ClearCheckBoxesHotkey)
				ClearCheckBoxes();
		}

		/// <summary>
		/// Opens the folder containing the current image and selects it.
		/// </summary>
		public void CurrentImageLabelClicked()
		{
			string param = "/select,";
			param += "\"" + (CurrentImage as BitmapImage).UriSource.LocalPath + "\"";
			Process.Start("explorer.exe", param);
		}

		/// <summary>
		/// Shows an error message and goes to the next image.
		/// </summary>
		/// <param name="ex">Exception with the error message.</param>
		private void HandleError(Exception ex)
		{
			System.Windows.MessageBox.Show("The image " + _imagePathList[_currentImageIndex].FullName + " caused this error: " + ex.Message + ".\r\nImage will be skipped.");
			_imagePathList.RemoveAt(_currentImageIndex);
			UpdatePictureBox();
		}

		/// <summary>
		/// Clears the filter of the CheckBoxes.
		/// </summary>
		public void ClearCheckBoxFilter()
		{
			CheckBoxFilter = "";
		}

		/// <summary>
		/// Clears the filter of the ImagePaths.
		/// </summary>
		public void ClearImagePathFilter()
		{
			ImagePathFilter = "";
		}

		/// <summary>
		/// Notifies the UI of image related changes.
		/// </summary>
		private void NotifyImagePropertyChanges()
		{
			NotifyOfPropertyChange(() => CanCopy);
			NotifyOfPropertyChange(() => ExecuteButtonColor);
			NotifyOfPropertyChange(() => ImageLoaded);
			NotifyOfPropertyChange(() => ImageWidth);
			NotifyOfPropertyChange(() => ImageHeight);
			NotifyOfPropertyChange(() => ImageFileName);
			NotifyOfPropertyChange(() => ImageFileSize);
			NotifyOfPropertyChange(() => CanReverseImageSearch);
		}

		/// <summary>
		/// Starts the reverse image search.
		/// </summary>
		public void StartReverseImageSearch()
		{
			if (!_reverseImageSearchWorker.IsBusy)
				_reverseImageSearchWorker.RunWorkerAsync();
		}

		/// <summary>
		/// Uploads the current image to google reverse image search.
		/// </summary>
		private void GoogleReverseImageSearch(object sender, DoWorkEventArgs e)
		{
			try
			{
				_dispatcher.Invoke(new System.Action(() => ReverseImageSearchButtonImage = new BitmapImage(new Uri("pack://application:,,,/SenpaiCopy;component/Resources/loading.gif"))));
				var client = new RestClient("http://imagebin.ca");
				client.Proxy = null;
				var request = new RestRequest("upload.php", Method.POST);
				request.AddParameter("key", "5yjR1+Mgnzh+Wa+ADwUFYaJ4CeUQHpSQ");
				request.AddParameter("dl_limit", 1);
				request.AddFile("file", _imagePathList[_currentImageIndex].FullName);
				var response = client.Execute(request);
				int index = response.Content.IndexOf("http");
				string imgUrl = response.Content.Substring(index, (response.Content.Length - 1) - index);
				Process.Start("https://www.google.com/searchbyimage?site=search&sa=X&image_url=" + imgUrl);
			}
			catch (Exception)
			{
				System.Windows.MessageBox.Show("Something went wrong while googling the image.");
			}
			finally
			{
				_dispatcher.Invoke(new System.Action(() => ReverseImageSearchButtonImage = new BitmapImage(new Uri("pack://application:,,,/SenpaiCopy;component/Resources/google-favicon.png"))));
			}
		}
	}
}