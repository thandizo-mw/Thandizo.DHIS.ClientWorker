using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Thandizo.DAL.Models;
using Thandizo.DataModels.General;
using Thandizo.DataModels.Integrations;

namespace Thandizo.DHIS.BLL.Services
{
    public class PatientService
    {
        private readonly thandizoContext _context;

        public PatientService(thandizoContext context)
        {
            _context = context;
        }

        public async Task<OutputResponse> PostToDhis(long patientId)
        {
            //get all dhis2 attribute mappings for patient
            var attributes = await _context.DhisIntegrations.Where(x => x.ModuleCode.Equals("PAT")).ToListAsync();

            //get patient details
            var patient = await _context.Patients.Where(x => x.PatientId.Equals(patientId))
                .Select(x => new DhisPatientIntegrationDTO
                {
                    DateofBirth = x.DateOfBirth,
                    FirstName = x.FirstName,
                    Gender = x.Gender,
                    HomeAddress = x.HomeAddress,
                    NationalId = x.IdentificationType.ExternalReferenceNumber.Equals("NID") ? x.IdentificationNumber : "",
                    OtherIdentificationNumber = x.IdentificationType.ExternalReferenceNumber.Equals("OIT") ? x.IdentificationNumber : "",
                    PassportNumber = x.IdentificationType.ExternalReferenceNumber.Equals("PST") ? x.IdentificationNumber : "",
                    LastName = x.LastName,
                    NationalityName = x.NationalityCodeNavigation.NationalityName,
                    NextOfKinFirstName = x.NextOfKinFirstName,
                    NextOfKinLastName = x.NextOfKinLastName,
                    NextOfKinPhoneNumber = x.NextOfKinPhoneNumber,
                    CountryName = x.ResidenceCountryCodeNavigation.CountryName
                }).FirstOrDefaultAsync();

            //preapre attributes for DHIS2 integration
            //************* START ************************************************
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
                var attributeId = attributes.FirstOrDefault(x => x.SourceColumnName.Equals(prop.Name)).DhisAttributeId;

                attributeItems.Add(new DhisTrackedEntityAttribute
                {
                    Attribute = attributeId,
                    Value = propertyValue
                });
            }
            //************* END ***********************************************

            return new OutputResponse
            {
                IsErrorOccured = false,
                Message = "Posted to DHIS2 successfully"
            };
        }
    }
}
