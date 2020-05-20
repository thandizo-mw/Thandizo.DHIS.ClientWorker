using AngleDimension.Standard.Http.HttpServices;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Thandizo.DAL.Models;
using Thandizo.DataModels.General;
using Thandizo.DataModels.Integrations;
using Thandizo.DataModels.Integrations.Responses;
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
            var patientId = statuses.FirstOrDefault().PatientId;
            var patient = await _context.Patients.FirstOrDefaultAsync(x => x.PatientId.Equals(patientId));            

            //symptoms as data elements
            var symptoms = from ds in statuses
                           join s in _context.PatientSymptoms on ds.SymptomId equals s.SymptomId
                           select new DhisDataValue
                           {
                               Value = "Yes",
                               DataElement = s.ExternalReferenceNumber
                           };

            //get program details
            var program = await _context.DhisPrograms.FirstOrDefaultAsync();

            //get organisation unit (facility)
            var organisationUnit = await _context.DhisOrganisationUnits
                .FirstOrDefaultAsync(x => x.DistrictCode.Equals(patient.DistrictCode));

            var trackedEntityInstance = new DhisTrackedEntityInstance
            {
                CompletedDate = DateTime.UtcNow.AddHours(2).Date,
                EventDate = DateTime.UtcNow.AddHours(2).Date,
                DataValues = symptoms,
                OrgUnit = organisationUnit.DhisOrgUnitId,
                TrackedEntityInstance = patient.ExternalReferenceNumber,
                Program = program.DhisProgramId,
                ProgramStage = program.DhisProgramStage,
                Status = "COMPLETED"
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

            var response = await HttpRequestFactory.Post($"{_dhisApiUrl}/events", trackedEntityInstance, headerFields);

            //handle response from DHIS2 api
            var dhisResponse = response.ContentAsType<DhisResponse>();

            if (!dhisResponse.HttpStatus.Equals("OK"))
            {
                var messages = string.Empty;
                foreach (var importSummary in dhisResponse.Response.ImportSummaries)
                {
                    messages = string.Join("; ", importSummary.Conflicts.Select(x => x.Value).ToArray());
                }
                throw new ArgumentException(messages);
            }

            return new OutputResponse
            {
                IsErrorOccured = false,
                Message = "Posted to DHIS2 successfully"
            };
        }
    }
}
