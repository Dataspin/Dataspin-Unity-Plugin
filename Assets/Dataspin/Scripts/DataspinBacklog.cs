using UnityEngine;

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using MiniJSON;

namespace Dataspin {
	public class DataspinTape {

		public static bool isDebug = true;

		public const string BacklogPreferenceKey = "DATASPIN_BACKLOG";
		//Key used in PlayerPrefs encryption, just like preferences key, shouldn't be changed after release 
		public static string ENCRYPTION_KEY = "39de5d3b2a503633"; 

		public void ReadBacklog() {
			string backlogString = PlayerPrefs.GetString(BacklogPreferenceKey);
			backlogString = Decrypt(backlogString);

			
		}

		public void PutRequestOnBacklog(DataspinWebRequest request) {

		}

		public static string Encrypt (string toEncrypt)
		{
			var startTime = Time.realtimeSinceStartup;
			byte[] keyArray = System.Text.UTF8Encoding.UTF8.GetBytes (ENCRYPTION_KEY);
			byte[] toEncryptArray = System.Text.UTF8Encoding.UTF8.GetBytes (toEncrypt);
			RijndaelManaged rDel = new RijndaelManaged ();
			rDel.Key = keyArray;
			rDel.Mode = CipherMode.ECB;
			rDel.Padding = PaddingMode.PKCS7;

			ICryptoTransform cTransform = rDel.CreateEncryptor ();
			byte[] resultArray = cTransform.TransformFinalBlock (toEncryptArray, 0, toEncryptArray.Length);
			// Log("Encryption of string.Length = "+toEncrypt.Length+" in "+((Time.realtimeSinceStartup - startTime)*1000).ToString("f6")+"ms");
			return Convert.ToBase64String (resultArray, 0, resultArray.Length);
		}
		 
		public static string Decrypt (string toDecrypt)
		{
			var startTime = Time.realtimeSinceStartup;
			byte[] keyArray = System.Text.UTF8Encoding.UTF8.GetBytes (ENCRYPTION_KEY);
			byte[] toEncryptArray = Convert.FromBase64String (toDecrypt);
			RijndaelManaged rDel = new RijndaelManaged ();
			rDel.Key = keyArray;
			rDel.Mode = CipherMode.ECB;
			rDel.Padding = PaddingMode.PKCS7;

			ICryptoTransform cTransform = rDel.CreateDecryptor ();
			byte[] resultArray = cTransform.TransformFinalBlock (toEncryptArray, 0, toEncryptArray.Length);
			// Log("Decryption of string.Length = "+toDecrypt.Length+" in "+((Time.realtimeSinceStartup - startTime)*1000).ToString("f6")+"ms");
			return System.Text.UTF8Encoding.UTF8.GetString (resultArray);
		}
	}
}
