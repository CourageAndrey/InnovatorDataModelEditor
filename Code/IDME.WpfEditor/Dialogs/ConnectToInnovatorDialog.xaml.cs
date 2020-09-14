using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Xml;

using Aras.IOM;

namespace IDME.WpfEditor.Dialogs
{
	public partial class ConnectToInnovatorDialog
	{
		public ConnectToInnovatorDialog()
		{
			InitializeComponent();
		}

		public string Server
		{
			get { return serverTextBox.Text; }
			set { serverTextBox.Text = value; }
		}

		public string Database
		{
			get { return databaseComboBox.Text; }
			set { databaseComboBox.Text = value; }
		}

		public string UserName
		{
			get { return userNameTextBox.Text; }
			set { userNameTextBox.Text = value; }
		}

		public string Password
		{
			get { return passwordBox.Password; }
			set { passwordBox.Password = value; }
		}

		private void checkConnectionClick(object sender, RoutedEventArgs e)
		{
			string error;
			HttpServerConnection serverConnection = null;
			try
			{
				serverConnection = IomFactory.CreateHttpServerConnection(
					Server,
					Database,
					UserName,
					Password);
				Innovator innovator = new Innovator(serverConnection);
				Item checkConnectionItem = innovator.applyAML("<AML><Item type=\"ItemType\" action=\"get\"><name>ItemType</name></Item></AML>");
				error = checkConnectionItem.isError()
					? checkConnectionItem.getErrorDetail()
					: null;
			}
			catch (Exception exception)
			{
				error = exception.ToString();
			}
			finally
			{
				if (serverConnection != null)
				{
					serverConnection.Logout();
				}
			}

			if (string.IsNullOrEmpty(error))
			{
				MessageBox.Show("Works fine", "OK", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			else
			{
				MessageBox.Show(error, "An error occured", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void okClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

		private void cancelClick(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
		}

		private void populateDatabasesClick(object sender, RoutedEventArgs e)
		{
			databaseComboBox.ItemsSource = getDatabases(Server);
		}

		private static ICollection<string> getDatabases(string serverUrl)
		{
			var databases = new List<string>();

			Stream requestStream = null;
			HttpWebResponse webResponse = null;
			Stream responseStream = null;
			StreamReader responseReader = null;
			try
			{
				string url = serverUrl.EndsWith("InnovatorServer.aspx")
					? serverUrl.Replace("InnovatorServer.aspx", "DBList.aspx")
					: string.Format(
						CultureInfo.InvariantCulture,
						"{0}{1}Server/DBList.aspx",
						serverUrl,
						serverUrl.EndsWith("/") ? string.Empty : "/");

				HttpWebRequest webRequest = (HttpWebRequest) WebRequest.Create(url);
				webRequest.Method = "POST";
				UTF8Encoding utf8 = new UTF8Encoding();
				string xmlHeader = "<?xml version='1.0' encoding='utf-8' ?>";
				byte[] bytesRequest = utf8.GetBytes(xmlHeader + "<GetDB/>");
				webRequest.Method = "POST";
				webRequest.ContentLength = bytesRequest.Length;
				webRequest.ContentType = "text/xml";
				webRequest.Credentials = CredentialCache.DefaultCredentials;

				requestStream = webRequest.GetRequestStream();
				requestStream.Write(bytesRequest, 0, bytesRequest.Length);

				webResponse = (HttpWebResponse) webRequest.GetResponse();
				if (webRequest.HaveResponse)
				{
					responseStream = webResponse.GetResponseStream();
					responseReader = new StreamReader(responseStream, utf8);
					string resultAml = responseReader.ReadToEnd();

					XmlDocument xmlDocument = new XmlDocument();
					xmlDocument.LoadXml(resultAml);
					foreach (XmlElement databaseNode in xmlDocument.SelectNodes("//DB")?.OfType<XmlElement>() ?? new XmlElement[0])
					{
						databases.Add(databaseNode.GetAttribute("id"));
					}
				}
			}
			catch (Exception e)
			{
				MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return databases;
			}
			finally
			{
				requestStream?.Close();
				responseReader?.Close();
				responseStream?.Close();
				webResponse?.Close();
			}

			return databases;
		}
	}
}
