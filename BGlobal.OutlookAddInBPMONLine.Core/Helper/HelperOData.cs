
using System;
using System.IO;
using System.Net;
using System.Xml.Linq;
using BGlobal.OutlookAddInBPMONLine.Core.Model;
using System.Runtime.Serialization.Formatters.Binary;
using System.Linq;
using System.Collections.Generic;
using System.Xml;

namespace BGlobal.OutlookAddInBPMONLine.Core.Helper
{
    public class HelperOData
    {
        CookieContainer AuthCookie = new CookieContainer();
        private string baseUri;
        private string authServiceUri;
        private string serverUriUsr;
        private static readonly XNamespace ds = "http://schemas.microsoft.com/ado/2007/08/dataservices";
        private static readonly XNamespace dsmd = "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata";
        private static readonly XNamespace atom = "http://www.w3.org/2005/Atom";

        /// <summary>
        /// http://localhost/BPMAssessment/ServiceModel/AuthService.svc/Login
        /// </summary>
        /// <param name="connect"></param>
        public HelperOData(bool connect = true)
        {
            baseUri = Properties.Settings.Default["Server"].ToString();
            authServiceUri = baseUri + @"/ServiceModel/AuthService.svc/Login";
            serverUriUsr = baseUri + @"/ServiceModel/EntityDataService.svc/";

            if (connect)
                GetConnectionBPM();
        }

        ~HelperOData()
        {
            try
            {
                CookieCollection cookieCollection = AuthCookie.GetCookies(new Uri(authServiceUri));
                WriteCookiesToDisk(AuthCookie);
            }
            catch { }
        }

        private void GetConnectionBPM()
        {
            try
            {
                AuthCookie = ReadCookiesFromDisk();
                LoginBPM();

            }
            catch { }
        }

        private bool LoginBPM()
        {

            try
            {
                var authRequest = HttpWebRequest.Create(authServiceUri) as HttpWebRequest;
                authRequest.Method = "POST";
                authRequest.ContentType = "application/json";
                authRequest.CookieContainer = AuthCookie;
                authRequest.Headers.Set("ForceUseSession", "true");

                try
                {
                    CookieCollection cookieCollection = AuthCookie.GetCookies(new Uri(authServiceUri));
                    string csrfToken = cookieCollection["BPMCSRF"].Value;
                    authRequest.Headers.Add("BPMCSRF", csrfToken);
                }
                catch { }


                string userName = Properties.Settings.Default.User;
                string userPassword = Properties.Settings.Default.Password;

                using (var requestStream = authRequest.GetRequestStream())
                {
                    using (var writer = new StreamWriter(requestStream))
                    {
                        writer.Write(@"{
                                ""UserName"":""" + userName + @""",
                                ""UserPassword"":""" + userPassword + @"""
                                }");

                    }
                }

                ResponseStatus status = null;
                using (var response = (HttpWebResponse)authRequest.GetResponse())
                {
                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        string responseText = reader.ReadToEnd();
                        status = new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<ResponseStatus>(responseText);
                    }

                }

                if (status != null)
                {
                    if (status.Code == 0)
                    {
                        WriteCookiesToDisk(AuthCookie);
                        return true;
                    }

                }
            }
            catch { }
            return false;
        }

        private void WriteCookiesToDisk(CookieContainer cookieJar)
        {
            string file = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "cookies.dat");
            using (Stream stream = File.Create(file))
            {
                try
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(stream, cookieJar);
                }
                catch (Exception e)
                {

                }
            }
        }

        private CookieContainer ReadCookiesFromDisk()
        {

            try
            {
                string file = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "cookies.dat");
                using (Stream stream = File.Open(file, FileMode.Open))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    return (CookieContainer)formatter.Deserialize(stream);
                }
            }
            catch (Exception e)
            {
                return new CookieContainer();
            }
        }

        public void RemoveFileCookie()
        {

            try
            {

                string file = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "cookies.dat");
                System.IO.File.Delete(file);

            }
            catch { }
        }

        public bool ValidateConnection()
        {

            try
            {
                string requestUri = serverUriUsr + "ContactCollection?$top=1";
                var request = HttpWebRequest.Create(requestUri) as HttpWebRequest;
                request.Method = "GET";
                CookieCollection cookieCollection = AuthCookie.GetCookies(new Uri(authServiceUri));
                string csrfToken = cookieCollection["BPMCSRF"].Value;
                string user = cookieCollection["UserName"].Value;

                request.CookieContainer = AuthCookie;
                request.Headers.Add("BPMCSRF", csrfToken);
                request.Headers.Add("UserName", user);

                using (var response = request.GetResponse())
                {
                    if (((HttpWebResponse)response).StatusCode == HttpStatusCode.OK)
                    {
                        return true;
                    }
                }

            }
            catch (Exception e)
            {

            }

            return false;
        }

        public string GetEntityIdByField(string table, string field, string value)
        {
            try
            {
                string requestUri = serverUriUsr + table + "Collection?$filter = " + field + " eq '" + value + "'";
                var request = HttpWebRequest.Create(requestUri) as HttpWebRequest;
                request.Method = "GET";
                CookieCollection cookieCollection = AuthCookie.GetCookies(new Uri(authServiceUri));
                string csrfToken = cookieCollection["BPMCSRF"].Value;
                request.CookieContainer = AuthCookie;
                request.Headers.Add("BPMCSRF", csrfToken);
                request.Headers.Set("ForceUseSession", "true");

                using (var response = request.GetResponse())
                {
                    if (((HttpWebResponse)response).StatusCode == HttpStatusCode.OK)
                    {
                        XDocument xmlDoc = XDocument.Load(response.GetResponseStream());
                        response.Close();

                        var items = from entry in xmlDoc.Descendants(ds + "Id")
                                    select entry.Value;

                        return items.FirstOrDefault();
                    }
                }

                return null;
            }
            catch
            {
                throw;
            }

        }

        /*public bool RecordToHistory(string mailForm, string mailTo, string subject, string body, DateTime SendDate, string EmailId, Outlook.Attachments Attachments)
        {
            bool result = false;
            mailForm = Helper.ValidateMail(mailForm);
            mailTo = Helper.ValidateMail(mailTo);
            var accounts = GetAccountsByMails(mailTo);

            string ActivityCategoryID = GetEntityIdByField("ActivityCategory", "Name", "Email");
            if (string.IsNullOrEmpty(ActivityCategoryID))
                ActivityCategoryID = GetEntityIdByField("ActivityCategory", "Name", "Correo electrónico");

            string ActivityTypeID = GetEntityIdByField("ActivityType", "Code", "Email");
            string ActivityStatusID = GetEntityIdByField("ActivityStatus", "Code", "Finished");
            string EmailStatusID = GetEntityIdByField("EmailSendStatus", "Code", "Sended");


            foreach (var item in accounts)
            {

                //CreateActivityWithAttachments(item.Id, string.Empty, mailForm, mailTo, subject, body, ActivityCategoryID, ActivityTypeID, ActivityStatusID, SendDate, EmailStatusID, EmailId, Attachments);
                result = true;
            }


            if (accounts.Count == 0)
            {
                var contacts = GetContactsByMail(mailTo);
                foreach (var item in contacts)
                {
                    //CreateActivityWithAttachments(item.AccountId, item.Id, mailForm, mailTo, subject, body, ActivityCategoryID, ActivityTypeID, ActivityStatusID, SendDate, EmailStatusID, EmailId, Attachments);
                    result = true;
                }
            }

            return result;
        }*/

        private List<Account> GetAccountsByMails(string mail)
        {
            try
            {
                string TypeId = GetEntityIdByField("CommunicationType", "Name", "Email");
                if (string.IsNullOrEmpty(TypeId))
                    TypeId = GetEntityIdByField("CommunicationType", "Name", "Correo electrónico");

                string requestUri = serverUriUsr + "AccountCommunicationCollection?$filter = Number eq '" + mail + "' and CommunicationType/Id eq guid'" + TypeId + "'";
                var request = HttpWebRequest.Create(requestUri) as HttpWebRequest;
                request.Method = "GET";
                CookieCollection cookieCollection = AuthCookie.GetCookies(new Uri(authServiceUri));
                string csrfToken = cookieCollection["BPMCSRF"].Value;
                request.CookieContainer = AuthCookie;
                request.Headers.Add("BPMCSRF", csrfToken);
                request.Headers.Set("ForceUseSession", "true");

                using (var response = request.GetResponse())
                {
                    if (((HttpWebResponse)response).StatusCode == HttpStatusCode.OK)
                    {


                        XDocument xmlDoc = XDocument.Load(response.GetResponseStream());
                        response.Close();

                        var items = from entry in xmlDoc.Descendants(atom + "entry")
                                    select new Account
                                    {
                                        Id = (entry.Element(atom + "content").Element(dsmd + "properties").Element(ds + "AccountId").Value)

                                    };


                        return items.ToList();


                    }
                }
                return new List<Account>();
            }
            catch
            {
                throw;
            }
        }

        private List<Contact> GetContactsByMail(string mail)
        {
            try
            {
                string requestUri = serverUriUsr + "ContactCollection?$filter = Email eq '" + mail + "'";
                var request = HttpWebRequest.Create(requestUri) as HttpWebRequest;
                request.Method = "GET";
                CookieCollection cookieCollection = AuthCookie.GetCookies(new Uri(authServiceUri));
                string csrfToken = cookieCollection["BPMCSRF"].Value;
                request.CookieContainer = AuthCookie;
                request.Headers.Add("BPMCSRF", csrfToken);
                request.Headers.Set("ForceUseSession", "true");



                using (var response = request.GetResponse())
                {
                    if (((HttpWebResponse)response).StatusCode == HttpStatusCode.OK)
                    {
                        XDocument xmlDoc = XDocument.Load(response.GetResponseStream());
                        response.Close();

                        var items = from entry in xmlDoc.Descendants(atom + "entry")
                                    select new Contact
                                    {
                                        Id = (entry.Element(atom + "content").Element(dsmd + "properties").Element(ds + "Id").Value),
                                        AccountId = (entry.Element(atom + "content").Element(dsmd + "properties").Element(ds + "AccountId").Value)


                                    };


                        return items.ToList();

                    }
                }


                return new List<Contact>();
            }
            catch
            {
                throw;
            }
        }

        /*private bool CreateActivityWithAttachments(string AccountID, string ContactID, string EmailFrom, string EmailTo, string subject, string Body, 
            string ActivityCategoryId, string TypeId, string ActivityStatusID, DateTime sendDate, string EmailStatusId, string EmailId, 
            Outlook.Attachments Attachments)
        {
            try
            {
                string FileTypeID = GetEntityIdByField("FileType", "Code", "File");

                Guid id = Guid.NewGuid();
                var content = new XElement(dsmd + "properties",
                            new XElement(ds + "Id", id.ToString()),
                             new XElement(ds + "Title", subject),
                            new XElement(ds + "Body", Body),
                            new XElement(ds + "ActivityCategoryId", ActivityCategoryId),
                            new XElement(ds + "TypeId", TypeId),
                            new XElement(ds + "Recepient", EmailTo),
                            new XElement(ds + "Sender", EmailFrom),
                            new XElement(ds + "StatusId", ActivityStatusID),
                            new XElement(ds + "SendDate", sendDate),
                            new XElement(ds + "IsHtmlBody", true),
                             new XElement(ds + "EmailSendStatusId", EmailStatusId),
                             new XElement(ds + "GlobalActivityID", EmailId)
                            );


                if (!string.IsNullOrEmpty(AccountID))
                    content.Add(new XElement(ds + "AccountId", AccountID));

                if (!string.IsNullOrEmpty(ContactID))
                    content.Add(new XElement(ds + "ContactId", ContactID));


                var entry = new XElement(atom + "entry",
                new XElement(atom + "content",
                new XAttribute("type", "application/xml"), content));

                var request = (HttpWebRequest)HttpWebRequest.Create(serverUriUsr + "ActivityCollection/");
                CookieCollection cookieCollection = AuthCookie.GetCookies(new Uri(authServiceUri));
                string csrfToken = cookieCollection["BPMCSRF"].Value;
                request.CookieContainer = AuthCookie;
                request.Headers.Add("BPMCSRF", csrfToken);
                request.Headers.Set("ForceUseSession", "true");


                request.Method = "POST";
                request.Accept = "application/atom+xml";
                request.ContentType = "application/atom+xml;type=entry";
                using (var writer = XmlWriter.Create(request.GetRequestStream()))
                {
                    entry.WriteTo(writer);
                }

                using (WebResponse responseFileCollection = request.GetResponse())
                {
                    if (((HttpWebResponse)responseFileCollection).StatusCode == HttpStatusCode.Created)
                    {
                        responseFileCollection.Close();

                        foreach (Outlook.Attachment item in Attachments)
                        {
                            string PR_ATTACH_DATA_BIN = "http://schemas.microsoft.com/mapi/proptag/0x37010102";
                            var attachmentData = item.PropertyAccessor.GetProperty(PR_ATTACH_DATA_BIN);
                            CreateActivityFile(id.ToString(), item.FileName, FileTypeID, attachmentData);
                        }
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                throw;
            }

        }*/

        private bool CreateActivityFile(string ActivityID, string filename, string fileTypeId, byte[] file)
        {
            try
            {
                Guid id = Guid.NewGuid();

                var content = new XElement(dsmd + "properties",
                new XElement(ds + "ActivityId", ActivityID),
                new XElement(ds + "Id", id.ToString()),
                new XElement(ds + "Name", filename),
                new XElement(ds + "TypeId", fileTypeId)
                );
                var entry = new XElement(atom + "entry",
                        new XElement(atom + "content",
                        new XAttribute("type", "application/xml"), content));

                var request = (HttpWebRequest)HttpWebRequest.Create(serverUriUsr + "ActivityFileCollection/");
                CookieCollection cookieCollection = AuthCookie.GetCookies(new Uri(authServiceUri));
                string csrfToken = cookieCollection["BPMCSRF"].Value;
                request.CookieContainer = AuthCookie;
                request.Headers.Add("BPMCSRF", csrfToken);
                request.Headers.Set("ForceUseSession", "true");
                request.Method = "POST";
                request.Accept = "application/atom+xml";
                request.ContentType = "application/atom+xml;type=entry";
                using (var writer = XmlWriter.Create(request.GetRequestStream()))
                {
                    entry.WriteTo(writer);
                }

                using (WebResponse responseFileCollection = request.GetResponse())
                {
                    if (((HttpWebResponse)responseFileCollection).StatusCode == HttpStatusCode.Created)
                    {
                        responseFileCollection.Close();
                        if (TransferFile(id, "Data", file))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            catch
            {
                throw;
            }

        }

        private Boolean TransferFile(Guid fileRecordId, string columnName, byte[] file)
        {

            try
            {

                var request = (HttpWebRequest)HttpWebRequest.Create(serverUriUsr + "ActivityFileCollection(guid'" + fileRecordId.ToString() + "')/" + columnName);
                CookieCollection cookieCollection = AuthCookie.GetCookies(new Uri(authServiceUri));
                string csrfToken = cookieCollection["BPMCSRF"].Value;
                request.CookieContainer = AuthCookie;
                request.Headers.Add("BPMCSRF", csrfToken);
                request.Headers.Set("ForceUseSession", "true");
                request.Method = "PUT";
                request.Accept = "application/octet-stream,application/json;odata=verbose";
                request.ContentType = "multipart/form-data;boundary=+++++";

                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(file, 0, file.Length);
                }

                using (WebResponse response = request.GetResponse())
                {
                    response.Close();
                    return true;
                }

                return false;
            }
            catch
            {
                throw;
            }
        }


        public List<Contact> GetContactByMails(List<string> list)
        {
            try
            {
                string mails = string.Empty;


                foreach (string mail in list)
                {
                    mails += " Email eq '" + Helper.ValidateMail(mail) + "' or";
                }
                string requestUri = serverUriUsr + "ContactCollection?$filter=" + mails.Substring(0, mails.Length - 3);
                var request = HttpWebRequest.Create(requestUri) as HttpWebRequest;
                request.Method = "GET";
                CookieCollection cookieCollection = AuthCookie.GetCookies(new Uri(authServiceUri));
                string csrfToken = cookieCollection["BPMCSRF"].Value;
                request.CookieContainer = AuthCookie;
                request.Headers.Add("BPMCSRF", csrfToken);
                request.Headers.Set("ForceUseSession", "true");



                using (var response = request.GetResponse())
                {
                    if (((HttpWebResponse)response).StatusCode == HttpStatusCode.OK)
                    {
                        XDocument xmlDoc = XDocument.Load(response.GetResponseStream());
                        response.Close();

                        var items = from entry in xmlDoc.Descendants(atom + "entry")
                                    select new Contact
                                    {
                                        Id = (entry.Element(atom + "content").Element(dsmd + "properties").Element(ds + "Id").Value),
                                        Email = (entry.Element(atom + "content").Element(dsmd + "properties").Element(ds + "Email").Value),
                                        Name = (entry.Element(atom + "content").Element(dsmd + "properties").Element(ds + "Name").Value),
                                        AccountName = GetEntityFieldByID("Account", "Name", (entry.Element(atom + "content").Element(dsmd + "properties").Element(ds + "AccountId").Value)),
                                        Image = GetByteEntityFieldById("SysImage", "Data", (entry.Element(atom + "content").Element(dsmd + "properties").Element(ds + "PhotoId").Value))
                                    };



                        return items.ToList();

                    }
                }


                return new List<Contact>();
            }
            catch (Exception ex)
            {
                throw;
            }



        }

        private string GetEntityFieldByID(string table, string field, string Id)
        {
            try
            {
                if (Id == Guid.Empty.ToString())
                {
                    return string.Empty;
                }

                string requestUri = serverUriUsr + table + "Collection(guid'" + Id + "')/" + field;
                var request = HttpWebRequest.Create(requestUri) as HttpWebRequest;
                request.Method = "GET";
                CookieCollection cookieCollection = AuthCookie.GetCookies(new Uri(authServiceUri));
                string csrfToken = cookieCollection["BPMCSRF"].Value;
                request.CookieContainer = AuthCookie;
                request.Headers.Add("BPMCSRF", csrfToken);
                request.Headers.Set("ForceUseSession", "true");

                using (var response = request.GetResponse())
                {
                    if (((HttpWebResponse)response).StatusCode == HttpStatusCode.OK)
                    {
                        XDocument xmlDoc = XDocument.Load(response.GetResponseStream());
                        response.Close();

                        var items = from entry in xmlDoc.Descendants(ds + field)
                                    select entry.Value;

                        return items.FirstOrDefault();
                    }
                }

                return null;

            }
            catch
            {
                throw;
            }
        }

        private byte[] GetByteEntityFieldById(string table, string field, string Id)
        {
            try
            {
                string requestUri = serverUriUsr + table + "Collection(guid'" + Id + "')/" + field; // +"";
                var request = HttpWebRequest.Create(requestUri) as HttpWebRequest;
                request.Method = "GET";

                CookieCollection cookieCollection = AuthCookie.GetCookies(new Uri(authServiceUri));
                string csrfToken = cookieCollection["BPMCSRF"].Value;
                request.CookieContainer = AuthCookie;
                request.Headers.Add("BPMCSRF", csrfToken);
                request.Headers.Set("ForceUseSession", "true");

                using (var response = request.GetResponse())
                {
                    if (((HttpWebResponse)response).StatusCode == HttpStatusCode.OK)
                    {
                        Stream sourceStream = response.GetResponseStream();
                        byte[] file;

                        if (sourceStream == null) { return null; }

                        using (MemoryStream ms = new MemoryStream())
                        {
                            sourceStream.CopyTo(ms);
                            response.Close();
                            file = ms.ToArray();
                        }
                        return file;
                    }
                }

            }
            catch { }

            return null;
        }
    }
}