﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Abstractions.Types;
using Core.DomainModel;
using Core.DomainModel.Qa.References;
using Core.DomainModel.References;
using Core.DomainServices.Repositories.Interface;
using Core.DomainServices.Repositories.Qa;
using Core.DomainServices.Repositories.Reference;
using Core.DomainServices.Time;
using Infrastructure.Services.Configuration;
using Infrastructure.Services.Http;

namespace Core.BackgroundJobs.Model.ExternalLinks
{
    public class CheckExternalLinksBackgroundJob : IAsyncBackgroundJob
    {
        private readonly IReferenceRepository _referenceRepository;
        private readonly IInterfaceRepository _interfaceRepository;
        private readonly IBrokenExternalReferencesReportRepository _reportRepository;
        private readonly IEndpointValidationService _endpointValidationService;
        private readonly KitosUrl _kitosUrl;
        private readonly IOperationClock _operationClock;

        public string Id => StandardJobIds.CheckExternalLinks;

        public CheckExternalLinksBackgroundJob(
            IReferenceRepository referenceRepository,
            IInterfaceRepository interfaceRepository,
            IBrokenExternalReferencesReportRepository reportRepository,
            IEndpointValidationService endpointValidationService,
            KitosUrl kitosUrl,
            IOperationClock operationClock)
        {
            _referenceRepository = referenceRepository;
            _interfaceRepository = interfaceRepository;
            _reportRepository = reportRepository;
            _endpointValidationService = endpointValidationService;
            _kitosUrl = kitosUrl;
            _operationClock = operationClock;
        }

        public async Task<Result<string, OperationError>> ExecuteAsync(CancellationToken token = default)
        {
            var brokenInterfaceLinks = await CheckInterfaceLinksAsync().ConfigureAwait(false);
            var brokenSystemLinks = await CheckSystemLinksAsync().ConfigureAwait(false);

            var report = new BrokenExternalReferencesReport
            {
                Created = _operationClock.Now,
                BrokenInterfaceLinks = brokenInterfaceLinks.ToList(),
                BrokenExternalReferences = brokenSystemLinks.ToList()
            };
            _reportRepository.ReplaceCurrentReport(report);
            return $"Created report with timestamp:{report.Created} and id '{report.Id}'. {report.BrokenExternalReferences.Count + report.BrokenInterfaceLinks.Count} broken links found.";
        }

        private async Task<IReadOnlyList<BrokenLinkInInterface>> CheckInterfaceLinksAsync()
        {
            var results = new List<BrokenLinkInInterface>();

            foreach (var itInterface in _interfaceRepository.GetInterfacesWithExternalReferenceDefined())
            {
                var url = itInterface.Url;
                if (Include(url))
                {
                    var validation = await _endpointValidationService.ValidateAsync(url).ConfigureAwait(false);
                    if (!validation.Success)
                    {
                        var item = new BrokenLinkInInterface();
                        SetResults(item, itInterface, validation.Error, url);
                        results.Add(item);
                    }
                }
            }

            return results;
        }

        private async Task<IReadOnlyList<BrokenLinkInExternalReference>> CheckSystemLinksAsync()
        {
            var results = new List<BrokenLinkInExternalReference>();

            foreach (var systemReference in _referenceRepository.GetByRootType(ReferenceRootType.System))
            {
                var url = systemReference.URL;
                if (Include(url))
                {
                    var validation = await _endpointValidationService.ValidateAsync(url).ConfigureAwait(false);
                    if (!validation.Success)
                    {
                        var item = new BrokenLinkInExternalReference();
                        SetResults(item, systemReference, validation.Error, url);
                        results.Add(item);
                    }
                }
            }

            return results;
        }

        private static void SetResults<T>(IBrokenLinkWithOrigin<T> item, T origin, EndpointValidationError error, string url) where T : IEntity
        {
            item.BrokenReferenceOrigin = origin;
            item.Cause = ParseCause(error.ErrorType);
            item.ErrorResponseCode = error.StatusCode.HasValue ? (int)error.StatusCode.Value : default(int?);
            item.ReferenceDateOfLatestLinkChange = origin.LastChanged;
            item.ValueOfCheckedUrl = url;
        }

        private static BrokenLinkCause ParseCause(EndpointValidationErrorType errorErrorType)
        {
            switch (errorErrorType)
            {
                case EndpointValidationErrorType.InvalidUriFormat:
                case EndpointValidationErrorType.InvalidWebsiteUri:
                    return BrokenLinkCause.InvalidUrl;
                case EndpointValidationErrorType.DnsLookupFailed:
                    return BrokenLinkCause.DnsLookupFailed;
                case EndpointValidationErrorType.CommunicationError:
                    return BrokenLinkCause.CommunicationError;
                case EndpointValidationErrorType.ErrorResponseCode:
                    return BrokenLinkCause.ErrorResponse;
                case EndpointValidationErrorType.TlsError:
                    return BrokenLinkCause.TlsError;
                default:
                    throw new ArgumentOutOfRangeException(nameof(errorErrorType), errorErrorType, null);
            }
        }

        private bool Include(string url)
        {
            //Url is not defined
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            //Url is a valid uri
            if (Uri.TryCreate(url, UriKind.Absolute, out var result))
            {
                //Ignore internal links
                return GetHost(_kitosUrl.Url).Equals(GetHost(result), StringComparison.OrdinalIgnoreCase) == false;
            }

            return false;
        }

        private static string GetHost(Uri url)
        {
            return url
                .Host
                .ToLowerInvariant()
                .Replace("www.", "");
        }
    }
}
