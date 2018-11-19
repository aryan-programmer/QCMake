using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Obj = System.Object;
using REA = System.Windows.RoutedEventArgs;
using SCEA = System.Windows.Controls.SelectionChangedEventArgs;
using StrDict = System.Collections.Generic.Dictionary<string , string>;
using TCEA = System.Windows.Controls.TextChangedEventArgs;

namespace QCMake
{
	/// </summary>
	public partial class MainWindow : Window
	{
		[Serializable]
		public class UnknownException : Exception
		{
			public UnknownException( ) { }
			public UnknownException( string message ) : base( message ) { }
			public UnknownException( string message , Exception inner ) : base( message , inner ) { }
			protected UnknownException(
			  System.Runtime.Serialization.SerializationInfo info ,
			  System.Runtime.Serialization.StreamingContext context ) : base( info , context ) { }
		}

		public const string literal_cs = "C#", literal_cpp = "C++17";

		public static readonly StrDict
			fileNameExtentions = new StrDict
			{
				[ "C#" ] = ".cs" ,
				[ "C++17" ] = "" ,
			}, langaugeSpecForLanguage = new StrDict
			{
				[ "C#" ] = "CS" ,
				[ "C++17" ] = "CPP17WithBoost" ,
			};
		public static readonly Dictionary<string , StrDict> QCExtention_Language_Extention = new Dictionary<string , StrDict>
		{
			{
				".qc" , new Dictionary<string, string>
				{
					{literal_cs,".cs" },
					{literal_cpp,".cpp" },
				}
			},
			{
				".sqc" , new Dictionary<string, string>
				{
					{literal_cs,".cs" },
					{literal_cpp,".cpp" },
				}
			},
			{
				".hqc" , new Dictionary<string, string>
				{
					{literal_cs,".cs" },
					{literal_cpp,".hpp" },
				}
			},
			{
				".uqc" , new Dictionary<string, string>
				{
					{ literal_cs,".cs" },
					{ literal_cpp,".hpp" },
				}
			}
		};
		public static readonly HashSet<string> validExtentions = new HashSet<string> { ".qc" , ".sqc" , ".hqc" , ".uqc" };

		public string OutputText
		{
			get => Output != null ? Output.Text ?? ( Output.Text = "" ) : "";
			set
			{
				if ( Output == null ) return;
				if ( Output.Text == null ) Output.Text = "";
				Output.Text = value;
				Output.ScrollToEnd();
			}
		}

		private void OutputLn( string s ) => OutputText += $"\n[{DateTime.Now}]: " + s;

		public MainWindow( ) => InitializeComponent();

		private static string ShowDialog<TDialog>( TDialog dialog ) where TDialog : CommonDialog
		{
			if ( dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK )
			{
				switch ( dialog )
				{
				case FileDialog file: return file.FileName;
				case FolderBrowserDialog folder:
					return folder.SelectedPath;
				default: return null;
				}
			}
			return null;
		}

		private void ChooseMakeExe_Location_Click( Obj sender , REA e ) =>
			MakeExe_Location.Text = ShowDialog( new OpenFileDialog() );

		private void ChooseCodeDir_Click( Obj sender , REA e ) => CodeDir.Text = ShowDialog( new FolderBrowserDialog() );

		private void ChooseQcExe_Location_Button_Click( Obj sender , REA e ) =>
			QcExe_Location.Text = ShowDialog( new OpenFileDialog() );

		private void Make_Makefile_Button_Click( Obj sender , REA e )
		{
			OutputLn( $@"Started creating makefile for ""{CodeDir.Text}""." );
			using ( StreamWriter writer = new StreamWriter( CodeDir.Text + @"\Makefile" ) )
			{
				// The language specified
				string language = 
					// Get the selected language, cast as a TextBlock with "as"
					( LanguageBox.SelectedItem as TextBlock )
					// Safely get the text
					?.Text ?? 
					// If the text is null we throw a ArgumentNullException
					throw new ArgumentNullException( "No language selected." );
				// Write the start variable values
				writer.Write(
					$@"BEGIN_VALUES = ""{QcExe_Location.Text}"" --file
ENDING_FLAGS = --language {langaugeSpecForLanguage[ language ]}

" );
				// The files with a valid extension
				FileInfo[ ] files =
					(
					// For all the files in the code directory
					from file in new DirectoryInfo( CodeDir.Text ).GetFiles()
					// Where the file's extension is a valid one.
					where validExtentions.Contains( file.Extension )
					// Select it
					select file
					).ToArray();
				string[ ] cnvFiles =
					(
					// For all the files
					from file in files
					// Remove the extension and add the new one.
					select file.Name.Substring(
						0 , file.Name.Length - file.Extension.Length ) +
						QCExtention_Language_Extention[ file.Extension ][ language ] ).ToArray();
				// For all the files 
				for ( int i = 0; i < files.Length; i++ )
				{
					// Write the appropriate make file target
					writer.Write( $@"{cnvFiles[ i ]}: {files[ i ].Name}
	$(BEGIN_VALUES) $< $(ENDING_FLAGS)

" );
				}
				// The main "all" target
				writer.Write( "all: " );
				// It's dependencies are all the files
				foreach ( var cnvFile in cnvFiles ) writer.Write( $@"{cnvFile} " );
				// The confirmation message and the start of the clean target
				writer.Write( @"
	echo ""All done.""

clean:
	del " );
				// Deletes all the files
				foreach ( var cnvFile in cnvFiles ) writer.Write( $@"{cnvFile} " );
			}
			// Final log
			OutputLn( $@"Finished creating makefile for ""{CodeDir.Text}""." );
		}

		private void Make_Project_Button_Click( Obj sender , REA e )
		{
			OutputLn( $@"Running makefile for ""{CodeDir.Text}""." );
			ProcessStartInfo processStartInfo = new ProcessStartInfo
			{
				UseShellExecute = false ,
				FileName = MakeExe_Location.Text ,
				Arguments = "all" ,
				WorkingDirectory = CodeDir.Text ,
				CreateNoWindow = true ,
				RedirectStandardOutput = true ,
				RedirectStandardError = true
			};
			Process proc = Process.Start( processStartInfo );
			proc.WaitForExit();
			OutputLn( proc.StandardOutput.ReadToEnd() );
			if ( proc.ExitCode != 0 )
			{
				string writeStr = $@"Process exited with exit code: {proc.ExitCode}.
Error:
{proc.StandardError.ReadToEnd()}";
				OutputLn( "Error: " + writeStr );
				OutputLn( $@"Failed to run makefile for ""{CodeDir.Text}""." );
				throw new UnknownException( writeStr );
			}
			OutputLn( $@"Finished running makefile for ""{CodeDir.Text}""." );
		}

		private void SaveOutput( Obj sender , REA e ) =>
			SaveOutput( ShowDialog(
				new FolderBrowserDialog
				{
					ShowNewFolderButton = true ,
					SelectedPath = CodeDir.Text
				} ) );

		private void SaveOutput( string folderPath )
		{
			var path = folderPath +
					$@"\QCMakeLogFor[{ProjectName.Text}].txt";
			OutputLn( $"Saved output to file." );
			using ( StreamWriter writer = new StreamWriter( path ) )
				writer.Write( Output.Text );
		}

		private void ClearOutput( Obj sender , REA e ) => Output.Text = "";

		private void Window_Loaded( Obj sender , REA e ) => Output.Text = $"[{DateTime.Now}]: Started QCMake session";

		private void CodeDir_TextChanged( Obj sender , TCEA e ) =>
			OutputLn( $@"Set scripts directory to ""{CodeDir.Text}""." );

		private void QcExe_Location_TextChanged( Obj sender , TCEA e ) =>
			OutputLn( $@"Set QC trans-compiler executable to ""{QcExe_Location.Text}""." );

		private void MakeExe_Location_TextChanged( Obj sender , TCEA e ) =>
			OutputLn( $@"Set make.exe to ""{MakeExe_Location.Text}""." );

		private void ProjectName_KeyDown( object sender , System.Windows.Input.KeyEventArgs e )
		{
			if(e.Key == System.Windows.Input.Key.Enter )
				OutputLn( $@"Set project name to ""{ProjectName.Text}""." );
		}

		private void LanguageBox_SelectionChanged( Obj sender , SCEA e ) => OutputLn( $"Changed language to {( LanguageBox.SelectedItem as TextBlock )?.Text ?? "Unknown"}." );

		private void Clean_Project_Button_Click( Obj sender , REA e )
		{
			OutputLn( $@"Cleaning generated files for ""{CodeDir.Text}""." );
			ProcessStartInfo processStartInfo = new ProcessStartInfo
			{
				UseShellExecute = false ,
				FileName = MakeExe_Location.Text ,
				Arguments = "clean" ,
				WorkingDirectory = CodeDir.Text ,
				CreateNoWindow = true ,
				RedirectStandardOutput = true ,
				RedirectStandardError = true
			};
			Process proc = Process.Start( processStartInfo );
			proc.WaitForExit();
			if ( proc.ExitCode != 0 )
			{
				string writeStr = $@"Process exited with exit code: {proc.ExitCode}.
Error:
{proc.StandardError.ReadToEnd()}";
				OutputLn( "Error: " + writeStr );
				OutputLn( $@"Failed to clean generated files for ""{CodeDir.Text}""." );
				throw new UnknownException( writeStr );
			}
			OutputLn( $@"Finished cleaning generated files for ""{CodeDir.Text}""." );
		}
	}
}
