using Hl7.Fhir.Model;
using Ihis.FhirEngine.Core;
using Ihis.FhirEngine.Core.Data;
using Ihis.FhirEngine.Core.Exceptions;
using Ihis.FhirEngine.Core.Models;
using Ihis.FhirEngine.Core.Search;
using System.Net;
using Task = System.Threading.Tasks.Task;

namespace Synapxe.FhirWebApi.Enrollment.Handlers
{
    [FhirHandlerClass(AcceptedType = nameof(CarePlan))]
    public class EnrollmentHandler
    {
        private readonly ISearchService enrollmentSearchService;
        private readonly IDataService enrollmentDataService;

        public EnrollmentHandler(
            ISearchService<CarePlan> enrollmentSearchService,
            IDataService<CarePlan> enrollmentDataService
            )
        {
            this.enrollmentSearchService = enrollmentSearchService;
            this.enrollmentDataService = enrollmentDataService;
        }

        [FhirHandler("PreCRUD_create_enrollment", HandlerCategory.PreCRUD, FhirInteractionType.OperationType, CustomOperation = "put-enrollment")]
        public async Task PreCRUDCreateEnrollmentAsync(IFhirContext context, CarePlan carePlan, CancellationToken cancellationToken)
        {
            var patientId = carePlan.Subject.Reference.Split('/').Last();
            SearchResult searchResultCarePlan = await enrollmentSearchService!.SearchAsync(nameof(CarePlan),
                new[]
                {
                    ("subject", ResourceKey.Create(nameof(Patient), patientId).ToString()),
                    ("category", "pophealth-enrolment"),
                    ("_sort", "-_lastUpdated"),
                    ("_count", "1")
                }, false, cancellationToken).ConfigureAwait(false);

            var enrollment = searchResultCarePlan.Results.SingleOrDefault()?.Resource.ToResource() as CarePlan;
            if (enrollment != null)
            {
                throw new BadRequestException("Enrollment already exists");
            }
        }

        [FhirHandler("CRUD_create_enrollment", HandlerCategory.CRUD, FhirInteractionType.OperationType, CustomOperation = "put-enrollment")]
        public async Task CRUDCreateEnrollmentAsync(IFhirContext context, CarePlan carePlan, CancellationToken cancellationToken)
        {
            carePlan.Id = null;
            carePlan.Status = RequestStatus.Completed;
            carePlan.Period.End = null;
            carePlan.Created = FhirDateTime.Now().ToString();
            await enrollmentDataService.UpsertAsync(carePlan, null, true, true, false, cancellationToken).ConfigureAwait(false);
        }

        [FhirHandler("PostCRUD_create_enrollment", HandlerCategory.PostCRUD, FhirInteractionType.OperationType, CustomOperation = "put-enrollment")]
        public async Task PostCRUDCreateEnrollmentAsync(IFhirContext context, CarePlan carePlan, CancellationToken cancellationToken)
        {
            OperationOutcome successOperationOutcome = new()
            {
                Issue = new List<OperationOutcome.IssueComponent>
                {
                    new()
                    {
                        Severity = OperationOutcome.IssueSeverity.Information,
                        Code = OperationOutcome.IssueType.Informational,
                        Details = new CodeableConcept
                        {
                            Text = "All OK"
                        }
                    }
                }
            };
            context.Response.AddResource(successOperationOutcome);
            context.Response.StatusCode = HttpStatusCode.OK;
        }
    }
}
