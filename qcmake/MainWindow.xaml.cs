using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Forms;

namespace qcmake
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private static string ToString( string[ ] fileNames )
		{
			StringBuilder retval = new StringBuilder();
			foreach ( string filename in fileNames )
			{
				retval.Append( filename );
				retval.Append( ",\n" );
			}
			retval.Remove( retval.Length - 2 , 2 );
			return retval.ToString();
		}

		public MainWindow( ) => InitializeComponent();

		private void ChooseMakeExe_Location_Click( object sender , RoutedEventArgs e )
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			DialogResult res = openFileDialog.ShowDialog();
			if ( res == System.Windows.Forms.DialogResult.OK )
			{
				MakeExe_Location.Text = openFileDialog.FileName;
			}
		}

		private void ChooseCodeDir_Click( object sender , RoutedEventArgs e )
		{
			FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog
			{ ShowNewFolderButton = true };
			DialogResult res = folderBrowserDialog.ShowDialog();
			if ( res == System.Windows.Forms.DialogResult.OK )
			{
				CodeDir.Text = folderBrowserDialog.SelectedPath;
			}
		}

		private void ChooseCodeFiles_Click( object sender , RoutedEventArgs e )
		{
			OpenFileDialog openFileDialog = new OpenFileDialog
			{
				Multiselect = true ,
				InitialDirectory = CodeDir.Text
			};
			DialogResult res = openFileDialog.ShowDialog();
			if ( res == System.Windows.Forms.DialogResult.OK )
			{
				CodeFiles.Text = ToString( openFileDialog.FileNames );
			}
		}

		private void ChooseExtraMakeFile_Button_Click( object sender , RoutedEventArgs e )
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			DialogResult res = openFileDialog.ShowDialog();
			if ( res == System.Windows.Forms.DialogResult.OK )
			{
				ExtraMakeFile.Text = openFileDialog.FileName;
			}
		}

		private void ChooseQcExe_Location_Button_Click( object sender , RoutedEventArgs e )
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			DialogResult res = openFileDialog.ShowDialog();
			if ( res == System.Windows.Forms.DialogResult.OK )
			{
				QcExe_Location.Text = openFileDialog.FileName;
			}
		}

		private void Make_Makefile_Button_Click( object sender , RoutedEventArgs e )
		{
			using ( StreamWriter writer = new StreamWriter( CodeDir.Text + @"\Makefile" ) )
			{
				writer.Write(
					$@"
BEGIN_VALUES = ""{QcExe_Location.Text}"" --file
ENDING_FLAGS = --language CS

" );
				DirectoryInfo dir = new DirectoryInfo( CodeDir.Text );
				FileInfo[ ] files =
					( from file in dir.GetFiles()
					  where file.Extension.Contains( "qc" )
					  select file ).ToArray();
				string[ ] csFiles =
					( from file in files
					  select file.Name.Remove( file.Name.Length - 3 , 3 ) + ".cs" ).ToArray();
				for ( int i = 0; i < files.Length; i++ )
				{
					writer.Write( $@"{csFiles[ i ]}: {files[ i ].Name}" );
					writer.Write( "\n\t$(BEGIN_VALUES) $< $(ENDING_FLAGS)\n\n" );
				}
				writer.Write( "all: " );
				for ( int i = 0; i < files.Length; i++ )
				{
					writer.Write( $@"{csFiles[ i ]}" );
					if ( i != files.Length - 1 ) writer.Write( ", " );
				}
				writer.Write( "\n\techo \"All done.\"\n\n" );
				writer.Write( "clean: \n\tdel " );
				for ( int i = 0; i < files.Length; i++ )
					writer.Write( $@"{csFiles[ i ]} " );
			}
		}

		private void Make_Project_Button_Click( object sender , RoutedEventArgs e )
		{

		}

		private void Clean_Project_Button_Click( object sender , RoutedEventArgs e )
		{

		}
	}
}
