﻿using Caliburn.Micro;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace SenpaiCopy
{
	/// <summary>
	/// ViewModel for the <see cref="SettingsView"/>.
	/// </summary>
	class SettingsViewModel : PropertyChangedBase
	{
		#region Member

		private Key _previousHotkey;
		private Key _executeHotkey;
		private Key _nextHotkey;
		private Key _clearCheckBoxesHotkey;

		private ObservableCollection<string> _enabledFormats;
		private ObservableCollection<string> _supportedFormats;
		private int _enabledIndex;
		private int _supportedIndex;

		#endregion

		#region Properties

		/// <summary>
		/// The hotkey to go to the previous image.
		/// </summary>
		public Key PreviousHotkey
		{
			get { return _previousHotkey; }
			set
			{
				_previousHotkey = value;
				NotifyOfPropertyChange(() => PreviousHotkey);
			}
		}

		/// <summary>
		/// The hotkey to execute.
		/// </summary>
		public Key ExecuteHotkey
		{
			get { return _executeHotkey; }
			set
			{
				_executeHotkey = value;
				NotifyOfPropertyChange(() => ExecuteHotkey);
			}
		}

		/// <summary>
		/// The hotkey to go to the next image.
		/// </summary>
		public Key NextHotkey
		{
			get { return _nextHotkey; }
			set
			{
				_nextHotkey = value;
				NotifyOfPropertyChange(() => NextHotkey);
			}
		}

		/// <summary>
		/// The hotkey to clear all CheckBoxes.
		/// </summary>
		public Key ClearCheckBoxesHotkey
		{
			get { return _clearCheckBoxesHotkey; }
			set
			{
				_clearCheckBoxesHotkey = value;
				NotifyOfPropertyChange(() => ClearCheckBoxesHotkey);
			}
		}

		/// <summary>
		/// The enabled formats.
		/// </summary>
		public ObservableCollection<string> EnabledFormats
		{
			get { return _enabledFormats; }
			private set { _enabledFormats = value; }
		}

		/// <summary>
		/// Supported formats.
		/// </summary>
		public ObservableCollection<string> SupportedFormats
		{
			get { return _supportedFormats; }
			private set { _supportedFormats = value; }
		}

		/// <summary>
		/// Index of the selected enabled format.
		/// </summary>
		public int EnabledIndex
		{
			get { return _enabledIndex; }
			set
			{
				_enabledIndex = value;
				NotifyOfPropertyChange(() => CanAddToSupported);
			}
		}

		/// <summary>
		/// Index of the selected supported format.
		/// </summary>
		public int SupportedIndex
		{
			get { return _supportedIndex; }
			set
			{
				_supportedIndex = value;
				NotifyOfPropertyChange(() => CanAddToEnabled);
			}
		}

		/// <summary>
		/// Gets wether its possible to add to the enabled formats.
		/// </summary>
		public bool CanAddToEnabled
		{
			get { return SupportedIndex != -1; }
		}

		/// <summary>
		/// Gets wether its possible to add to the supported formats.
		/// </summary>
		public bool CanAddToSupported
		{
			get { return EnabledIndex != -1; }
		}

		#endregion

		/// <summary>
		/// Ctor.
		/// </summary>
		public SettingsViewModel()
		{
			LoadSettings();
		}

		/// <summary>
		/// Loads the saved settings.
		/// </summary>
		private void LoadSettings()
		{
			PreviousHotkey = (Key)Properties.Settings.Default.PreviousHotkey;
			ExecuteHotkey = (Key)Properties.Settings.Default.ExecuteHotkey;
			NextHotkey = (Key)Properties.Settings.Default.NextHotkey;
			ClearCheckBoxesHotkey = (Key)Properties.Settings.Default.ClearCheckBoxesHotkey;

			EnabledFormats = new ObservableCollection<string>(Properties.Settings.Default.EnabledFormats.Split(';').OrderBy(i => i));
			List<string> tempSupportedFormats = new List<string>(Properties.Settings.Default.SupportedFormats.Split(';'));
			SupportedFormats = new ObservableCollection<string>(tempSupportedFormats.Where(i => !EnabledFormats.Contains(i)).OrderBy(i => i));
		}

		/// <summary>
		/// Adds the selected supported format to the <see cref="EnabledFormats"/>.
		/// </summary>
		public void AddToEnabled()
		{
			EnabledFormats.Add(SupportedFormats[SupportedIndex]);
			SupportedFormats.RemoveAt(SupportedIndex);
		}

		/// <summary>
		/// Adds the selected enabled format to the <see cref="SupportedFormats"/>.
		/// </summary>
		public void AddToSupported()
		{
			SupportedFormats.Add(EnabledFormats[EnabledIndex]);
			EnabledFormats.RemoveAt(EnabledIndex);
		}

		/// <summary>
		/// Save settings and close the window.
		/// </summary>
		/// <param name="sender">Should be the <see cref="SettingsView"/>, but is the clicked button for some reason.</param>
		public void Save(object sender)
		{
			Properties.Settings.Default.PreviousHotkey = (int)PreviousHotkey;
			Properties.Settings.Default.ExecuteHotkey = (int)ExecuteHotkey;
			Properties.Settings.Default.NextHotkey = (int)NextHotkey;
			Properties.Settings.Default.ClearCheckBoxesHotkey = (int)ClearCheckBoxesHotkey;

			Properties.Settings.Default.EnabledFormats = string.Join(";", EnabledFormats.Select(i => i.ToString()).ToArray());
			Properties.Settings.Default.Save();
			(((sender as FrameworkElement).Parent as FrameworkElement).Parent as Window).Close(); //TODO ULTRA HACKY CHECK THIS!!!!!
		}

		/// <summary>
		/// Restore settings and close the window.
		/// </summary>
		/// <param name="sender">Should be the <see cref="SettingsView"/>, but is the clicked button for some reason.</param>
		public void Cancel(object sender)
		{
			LoadSettings();
			(((sender as FrameworkElement).Parent as FrameworkElement).Parent as Window).Close(); //TODO ULTRA HACKY CHECK THIS!!!!!
		}
	}
}