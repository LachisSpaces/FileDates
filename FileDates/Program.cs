using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.IO;
using System;


namespace FileDates
{
   class Program
   {
      //const int _PropertyTagEquipMake = 0x010F;
      //const int _PropertyTagEquipModel = 0x110;
      const int _PropertyTagDateTime = 0x0132;

      static string _strApplicationPath = null;

      static void Main(string[] args)
      {
         _strApplicationPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\";
         string strError = "Photos processed succesfully";

         Console.WriteLine("Processing photos ...");
         Console.WriteLine();

         try
         {  // File-Info lesen und verarbeiten
            DirectoryInfo dir = new DirectoryInfo(_strApplicationPath);

            FileInfo[] fiSourceFiles = new string[] { "*.jpg", "*.mpo" }
            .SelectMany(i => dir.GetFiles(i, SearchOption.AllDirectories))
            .Distinct().ToArray();

            foreach (FileInfo fi in fiSourceFiles)
            {
               // Bitmap für das Foto erzeugen
               Bitmap bmFoto = new Bitmap(fi.FullName);

               // Name der Foto-Datei ausgeben
               Console.WriteLine(fi.Name);

               //// Hersteller und Name des Geräts ermitteln, über den das Foto erzeugt wurde
               //string strEquipmentManufacturer = null, strEquipmentModel = null;
               //if (GetTagValueAsString(bmFoto, _PropertyTagEquipMake, out strEquipmentManufacturer) && GetTagValueAsString(bmFoto, _PropertyTagEquipModel, out strEquipmentModel))
               //   Console.WriteLine("Manufactorer, Model: {0} {1}", strEquipmentManufacturer, strEquipmentModel);

               // Datum der Erzeugung des Fotos ermitteln
               DateTime dtmImageCreateDate;
               bool blnHasCreateDate = GetTagValueAsDateTime(bmFoto, _PropertyTagDateTime, out dtmImageCreateDate);

               // Bitmap freigeben, damit die Ressourcen nicht in Windows reserviert bleiben (was bei meinen Tests leider öfter der Fall war)
               bmFoto.Dispose();

               // Datum der Datei anpassen
               if (blnHasCreateDate)
               {
                  fi.CreationTime = dtmImageCreateDate;
                  fi.LastWriteTime = dtmImageCreateDate;
                  Console.WriteLine("Photo taken: {0}", dtmImageCreateDate.ToString());
               }
               Console.WriteLine();
            }
         }
         catch (Exception e)
         {
            LogException(e);
            strError = "Error: See log";
         }

#if DEBUG
         Console.WriteLine(strError);
         string s1 = Console.ReadLine();
#endif
      }


      /// <summary>
      /// Liefert den Wert einer Tag-Eigenschaft eines Bildes als String zurück
      /// </summary>
      private static bool GetTagValueAsString(Bitmap bitmap, int itemType, out string strTag)
      {
         string strResult = null;
         for (int i = 0; i < bitmap.PropertyItems.Length; i++)
         {
            PropertyItem item = bitmap.PropertyItems[i];
            if (item.Id == itemType)
            {
               for (int j = 0; j < item.Len - 1; j++)
                  strResult += (char)item.Value[j];
               break;
            }
         }
         strTag = strResult;
         return !string.IsNullOrEmpty(strResult);
      }

      /// <summary>
      /// Liefert den Wert einer Tag-Eigenschaft eines Bildes als DateTime zurück
      /// </summary>
      private static bool GetTagValueAsDateTime(Bitmap bitmap, int itemType, out DateTime dtmDate)
      {
         string strTag = null;
         if (GetTagValueAsString(bitmap, itemType, out strTag))
         {
            // Versuch, den im Format yyyy:MM:dd hh:mm:ss ermittelten String in ein Datum zu konvertieren
            if (strTag != "0000:00:00 00:00:00")
            {
               try
               {
                  int intYear = Convert.ToInt32(strTag.Substring(0, 4));
                  int intMonth = Convert.ToInt32(strTag.Substring(5, 2));
                  int intDay = Convert.ToInt32(strTag.Substring(8, 2));
                  int intHour = Convert.ToInt32(strTag.Substring(11, 2));
                  int intMinute = Convert.ToInt32(strTag.Substring(14, 2));
                  int intSecond = Convert.ToInt32(strTag.Substring(17, 2));
                  dtmDate = new DateTime(intYear, intMonth, intDay, intHour, intMinute, intSecond);
                  return true;
               }
               catch (Exception e) { LogException(e); }
            }
         }
         dtmDate = new DateTime(0);
         return false;
      }


      private static void LogException(Exception e)
      {
         string strLastExceptionMessage = e.Message;
         string strExceptionStackTrace = e.StackTrace;
         Exception eInnerException = e.InnerException;
         System.Text.StringBuilder msg = new System.Text.StringBuilder("----An error occured----\r\n");
         msg.Append(e.Message);
         while (eInnerException != null)
         {
            if (strLastExceptionMessage != eInnerException.Message)
            {
               strLastExceptionMessage = eInnerException.Message;
               msg.AppendFormat("\r\n\r\n----Inner error----\r\n{0}", strLastExceptionMessage);
            }
            strExceptionStackTrace = eInnerException.StackTrace;
            eInnerException = eInnerException.InnerException;
         }
         msg.AppendFormat("\r\n\r\n----Stacktrace----\r\n{0}", strExceptionStackTrace);
         StreamWriter sw = new StreamWriter(string.Format("{0}{1}_Error.txt", _strApplicationPath, System.DateTime.Now.ToString("yyyyMMdd_HHhmmss")));
         sw.Write(msg.ToString());
         sw.Close();
         sw.Dispose();
      }
   }
}
