using AngleDimension.Standard.Http.HttpServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Thandizo.DAL.Models;
using Thandizo.DataModels.General;
using Thandizo.DataModels.Integrations;
using Thandizo.DataModels.Patients;

namespace Thandizo.DHIS.BLL.Services
{
    public class PatientDailyStatusService : IPatientDailyStatusService
    {
        private readonly thandizoContext _context;
        private readonly string _dhisApiUrl;
        private readonly string _clientUserId;
        private readonly string _clientPassword;

        public PatientDailyStatusService(thandizoContext context, string dhisApiUrl,
            string clientUserId, string clientPassword)
        {
            _context = context;
            _dhisApiUrl = dhisApiUrl;
            _clientPassword = clientPassword;
            _clientUserId = clientUserId;
        }

        public async Task<OutputResponse> Post(IEnumerable<PatientDailyStatusDTO> statuses)
        {
            //get dhis2 reference number
            var patientId = statuses.FirstOrDefault().PatientId;
            var referenceNumber = _context.Patients.FirstOrDefault(x => x.PatientId.Equals(patientId)).ExternalReferenceNumber;

            //symptoms as data elements
            var symptoms = from ds in statuses
                           join s in _context.PatientSymptoms on ds.SymptomId equals s.SymptomId
                           select new DhisDataValue
                           {
                               Value = "1",
                               DataElement = s.ExternalReferenceNumber
                           };

            //post to dhis through basic authentication
            //***************************************
            byte[] credentialsBytes = Encoding.UTF8.GetBytes($"{_clientUserId}:{_clientPassword}");
            var credentials = Convert.ToBase64String(credentialsBytes);

            var headerFields = new List<HttpCustomHeaderField>
            {
                new HttpCustomHeaderField
                {
                    HeaderName = "Authorization",
                    HeaderValue = $"Basic { credentials }"
                }
            };

            return new OutputResponse
            {
                IsErrorOccured = false,
                Message = "Posted to DHIS2 successfully"
            };
        }
    }
}
