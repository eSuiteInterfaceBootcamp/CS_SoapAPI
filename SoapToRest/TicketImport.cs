using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using eSuiteApiWrapper;
using SoapToRest.TicketAPI;
using System.Net;
using System.IO;

namespace SoapToRest
{
    public class TicketImport
    {
        private TicketAPIClient _ticketApi;
        private RestApi _eSuiteApi;

        public TicketImport
        (
            string ticketApiURL, string ticketUsername, string ticketPassword,
            string eSuiteApiURL, string eSuiteApiAuthToken
        )
        {
            _eSuiteApi = new RestApi(eSuiteApiURL, eSuiteApiAuthToken);
            _ticketApi = new TicketAPIClient();
            _ticketApi.ClientCredentials.UserName.UserName = ticketUsername;
            _ticketApi.ClientCredentials.UserName.Password = ticketPassword;
        }

        public static string GetEcourtUserAuthToken(string username, string password)
        {
            return RestApi.GetUserAuthToken(username, password);
        }
        public static string CreateUser(string username)
        {
            return createUser(username);
        }
        public void ImportNewTickets()
        {
            Ticket[] tickets;

            try
            {
                tickets = _ticketApi.getNewTickets();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print(ex.Message);
                System.Diagnostics.Debug.Print(ex.InnerException.Message);
                throw ex;
            }

            string personJson, caseJson;

            foreach (Ticket ticket in tickets)
            {
                if (getJsonFromTicket(ticket, out personJson, out caseJson))
                {
                    int personId;
                    _eSuiteApi.InsertData("entities/person", personJson, out personId);

                    if (personId > 0)
                    {
                        int caseId;
                        _eSuiteApi.InsertData("entities/case", caseJson, out caseId);

                        if (caseId > 0)
                        {
                            int partyId;
                            _eSuiteApi.InsertData("entities/party", getPartyJson(caseId, personId), out partyId);

                            if (partyId > 0)
                            {
                                string chargeJson = getChargeJson(ticket, partyId);

                                if (chargeJson != null)
                                    _eSuiteApi.InsertData("entities/charge", chargeJson);
                            }
                        }
                    }
                }
            }
        }


        private static string createUser(string username)
        {
            WebRequest request = WebRequest.Create("https://customdev.journaltech.com/api/rest/signup/" + username);

            if (request is HttpWebRequest == false)
            {
                throw new Exception("Incorrect WebRequest type returned.");
            }

            request.Method = "POST";
            request.ContentType = "application / json";
            request.ContentLength = 0;
            request.Timeout = 10000;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                if (response.StatusCode != HttpStatusCode.Created)
                {
                    throw new Exception("API call failed with status code: " + response.StatusCode + ".");
                }

                using (var stream = response.GetResponseStream())
                {
                    if (stream != null)
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            string data = reader.ReadToEnd();

                            // parse password

                            return data;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }
        private bool getJsonFromTicket(Ticket ticket, out string personJson, out string caseJson)
        {
            personJson = "{ \"firstName\": \"";
            personJson += ticket.FirstName;
            personJson += "\", \"middleName\": \"";
            personJson += ticket.MiddleName;
            personJson += "\", \"lastName\": \"";
            personJson += ticket.LastName;
            personJson += "\", \"identifications\": [ { \"identificationClass\": \"STID\", \"identificationType\": \"DL\", \"identificationNumber\": \"";
            personJson += ticket.DriversLicenseNumber;
            personJson += "\" } ] }";

            caseJson = "{ \"caseNumber\": \"";
            caseJson += ticket.TicketNumber;
            caseJson += "\" }";

            return true;
        }
        private string getPartyJson(int caseId, int personId)
        {
            string json = "{ \"case\": { \"id\": ";
            json += caseId.ToString();
            json += " }, \"person\": { \"id\": ";
            json += personId.ToString();
            json += " } }";

            return json;
        }
        private string getChargeJson(Ticket ticket, int partyId)
        {
            // get statute id
            string searchPath = "statute/sectionNumber/" + ticket.StatuteCode;
            int statuteId;
            _eSuiteApi.SearchByGet(searchPath, out statuteId);

            if (statuteId < 1)
            {
                return null;
            }

            string json = "{ \"ticketNumber\": \"";
            json += ticket.TicketNumber;
            json += "\", \"location\": \"";
            json += ticket.Location;
            json += "\", \"associatedParty\": { \"id\": ";
            json += partyId.ToString();
            json += " }, \"chargeDate\": \"";
            json += getJsonDateString(ticket.TicketDate);
            json += "\", \"statute\": { \"id\": ";
            json += statuteId.ToString();
            json += " } }";

            return json;
        }
        private string getJsonDateString(DateTime dt)
        {
            // example format: 2016-03-03T00:00:00.000-0700",
            return (dt.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz"));
        }
    }
}
