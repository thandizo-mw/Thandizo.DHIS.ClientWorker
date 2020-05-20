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

namespace Thandizo.DHIS.BLL.Services
{
    public class PatientService : IPatientService
    {
        private readonly thandizoContext _context;
        private readonly string _dhisApiUrl;
        private readonly string _clientUserId;
        private readonly string _clientPassword;

        public PatientService(thandizoContext context, string dhisApiUrl,
            string clientUserId, string clientPassword)
        {
            _context = context;
            _dhisApiUrl = dhisApiUrl;
            _clientPassword = clientPassword;
            _clientUserId = clientUserId;
        }

        public async Task<OutputResponse> Post(long patientId)
        {
            //get all dhis2 attribute mappings for patient
            var attributes = await _context.DhisAttributes.Where(x => x.ModuleCode.Equals("PAT")).ToListAsync();

            //1. GET PATIENT DETAILS
            //************* START ************************************************
            var patient = await _context.Patients.Where(x => x.PatientId.Equals(patientId))
                .Select(x => new DhisPatientIntegrationDTO
                {
                    DateofBirth = x.DateOfBirth.Date,
                    FirstName = x.FirstName,
                    Gender = x.Gender.Equals("F") ? "female" : "male",
                    HomeAddress = x.HomeAddress,
                    NationalId = x.IdentificationType.ExternalReferenceNumber.Equals("NID") ? x.IdentificationNumber : "",
                    PassportNumber = x.IdentificationType.ExternalReferenceNumber.Equals("PST") ? x.IdentificationNumber : "",
                    LastName = x.LastName,
                    NationalityName = x.NationalityCodeNavigation.ExternalReferenceNumber,
                    NextOfKinFirstName = x.NextOfKinFirstName,
                    NextOfKinLastName = x.NextOfKinLastName,
                    NextOfKinPhoneNumber = x.NextOfKinPhoneNumber,
                    CountryName = x.ResidenceCountryCodeNavigation.ExternalReferenceNumber,
                    PatientAge = (int)((x.DateCreated.Subtract(x.DateOfBirth.Date).TotalDays) / 365),
                    DistrictName = x.DistrictCodeNavigation.DistrictName,
                    PhoneNumber = x.PhoneNumber,
                    PhysicalAddress = x.PhysicalAddress,
                    DistrictCode = x.DistrictCode,
                    ExternalReferenceNumber = x.ExternalReferenceNumber
                }).FirstOrDefaultAsync();

            //this avoid duplicated entries to DHIS2
            if (!string.IsNullOrEmpty(patient.ExternalReferenceNumber))
            {
                return new OutputResponse
                {
                    IsErrorOccured = false,
                    Message = "Already posted to DHIS2"
                };
            }

            //prepare attributes for DHIS2 integration            
            var attributeItems = new List<DhisTrackedEntityAttribute>();

            foreach (var prop in patient.GetType().GetProperties())
            {
                string propertyValue = string.Empty;

                if (prop.PropertyType == typeof(DateTime) || prop.PropertyType == typeof(DateTime?))
                {
                    propertyValue = string.Format("{0:yyyy-MM-dd}", prop.GetValue(patient, null));
                }
                else
                {
                    propertyValue = prop.GetValue(patient, null).ToString();
                }

                //get dhis attribute id
                var attribute = attributes.FirstOrDefault(x => x.SourceColumnName.ToLower().Equals(prop.Name.ToLower()));

                if (attribute != null && !string.IsNullOrEmpty(propertyValue))
                {
                    attributeItems.Add(new DhisTrackedEntityAttribute
                    {
                        Attribute = attribute.DhisAttributeId,
                        Value = propertyValue
                    });
                }
            }
            //************* END ***********************************************

            //2. GET SYMPTOMS FOR THE PATIENT : these are represented as data elements
            //************* START ************************************************
            var symptoms = _context.PatientDailyStatuses.Where(x => x.PatientId == patientId)
                    .Select(x => new DhisDataValue
                    {
                        Value = "Yes",
                        DataElement = x.Symptom.ExternalReferenceNumber
                    });
            //************* END ***********************************************

            //get program details
            var program = await _context.DhisPrograms.FirstOrDefaultAsync();

            //get organisation unit (facility)
            var organisationUnit = await _context.DhisOrganisationUnits
                .FirstOrDefaultAsync(x => x.DistrictCode.Equals(patient.DistrictCode));

            //adding enrollments
            var enrollments = new List<DhisEnrollment>(){
                new DhisEnrollment
                {
                     EnrollmentDate = DateTime.UtcNow.AddHours(2).Date,
                     IncidentDate = DateTime.UtcNow.AddHours(2).Date,
                     OrgUnit = organisationUnit.DhisOrgUnitId,
                     Program = program.DhisProgramId,
                     Events = new List<DhisEvent>()
                     {
                         new DhisEvent
                         {
                             DataValues = symptoms,
                             EventDate = DateTime.UtcNow.AddHours(2).Date,
                             OrgUnit = organisationUnit.DhisOrgUnitId,
                             Program = program.DhisProgramId,
                             ProgramStage = program.DhisProgramStage,
                             Status = "COMPLETED",
                             StoredBy = _clientUserId
                         }
                     }
                }
            };

            var trackedEntity = new DhisTrackedEntity()
            {
                Attributes = attributeItems,
                OrgUnit = organisationUnit.DhisOrgUnitId,
                TrackedEntityType = program.DhisTrackedEntityId,
                Enrollments = enrollments
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

            var response = await HttpRequestFactory.Post($"{_dhisApiUrl}/trackedEntityInstances",
               trackedEntity, headerFields);

            //handle response from DHIS2 api
            var dhisResponse = response.ContentAsType<DhisResponse>();

            if (dhisResponse.HttpStatus.Equals("OK"))
            {
                //update patient details
                var patientToUpdate = await _context.Patients.FirstOrDefaultAsync(x => x.PatientId.Equals(patientId));
                patientToUpdate.ExternalReferenceNumber = dhisResponse.Response.ImportSummaries.FirstOrDefault().Reference;
                await _context.SaveChangesAsync();
            }
            else
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
