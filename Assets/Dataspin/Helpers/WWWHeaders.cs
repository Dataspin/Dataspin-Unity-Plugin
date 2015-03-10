using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class WWWHeaders
{
    public static string CreateAuthorization(string aUserName, string aPassword)
    {
        return "Basic " + System.Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(aUserName + ":" + aPassword));
    }
   
    public static Hashtable AddAuthorizationHeader(this Hashtable aHeaders, string aUserName, string aPassword)
    {
        aHeaders.Add("Authorization",CreateAuthorization(aUserName, aPassword));
        return aHeaders;
    }

    public static Hashtable TokenAuthorization(this Hashtable hashtable, string token) {
    	hashtable.Add("Authorization", "Token " + System.Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(token)));
    	Debug.Log("Token added: Token "+System.Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(token)));
    	return hashtable;
    }
}