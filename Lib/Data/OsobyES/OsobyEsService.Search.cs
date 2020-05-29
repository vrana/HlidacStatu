﻿using Devmasters.Core;
using HlidacStatu.Lib.Searching;
using Nest;
using System;

namespace HlidacStatu.Lib.Data.OsobyES
{
    public static partial class OsobyEsService
    {
        
        public static OsobaEsSearchResult FulltextSearch(string query, int page, int pageSize, int? status = null)
        {

            page = page - 1 < 0 ? 0 : page - 1;

            var sw = new StopWatchEx();
            sw.Start();
            

            ISearchResponse<OsobaES> res = null;
            try
            {
                if(status.HasValue)
                {
                    res = _esClient
                        .Search<OsobaES>(s => s
                        .Size(pageSize)
                        .From(page * pageSize)
                        .Query(_query => _query
                            .Bool(_bool => _bool
                                .Must(_must => _must
                                    .Fuzzy(_fuzzy => _fuzzy
                                        .Field(_field => _field.FullName)
                                        .Value(query)
                                        .Fuzziness(Fuzziness.EditDistance(2))
                                    )
                                    && _must.Term(_field => _field.Status, status.Value)
                                )
                                .Should(
                                    _boostWomen => _boostWomen
                                    .Match(_match => _match
                                        .Field(_field => _field.FullName)
                                        .Query(query)
                                    ),
                                    _boostExact => _boostExact
                                    .Match(_match => _match
                                        .Field("fullName.lower")
                                        .Query(query)
                                    ),
                                    _boostAscii => _boostAscii
                                    .Match(_match => _match
                                        .Field("fullName.lowerascii")
                                        .Query(query)
                                    )
                                )
                            )
                        )
                        .TrackTotalHits(true)
                );
                }
                else
                {

                    res = _esClient //.MultiSearch<OsobaES>(s => s
                        .Search<OsobaES>(s => s
                        .Size(pageSize)
                        .From(page * pageSize)
                        .Query(_query => _query
                                .MultiMatch(c => c
                            .Fields(f => f
                                .Field(p => p.FullName)
                                .Field("fullName.lower",2)
                                .Field("fullName.lowerascii",1.5)
                                )
                            .Type(TextQueryType.MostFields)
                            .Fuzziness(Fuzziness.EditDistance(2))
                            .Query(query)
                            )
                        )
                        .TrackTotalHits(true)
                    );
                }

            }
            catch (Exception e)
            {
                Audit.Add(Audit.Operations.Search, "", "", "OsobaES", "error", query, null);
                if (res != null && res.ServerError != null)
                {
                    ES.Manager.LogQueryError<OsobaES>(res, "Exception, Orig query:"
                        + query + "   query:"
                        + query
                        , ex: e);
                }
                else
                {
                    HlidacStatu.Util.Consts.Logger.Error("", e);
                }
                throw;
            }
            sw.Stop();

            Audit.Add(Audit.Operations.Search, "", "", "OsobaES", res.IsValid ? "valid" : "invalid", query, null);

            if (res.IsValid == false)
            {
                ES.Manager.LogQueryError<OsobaES>(res, "Exception, Orig query:"
                    + query + "   query:"
                    + query
                    );
            }

            var search = new OsobaEsSearchResult
            {
                OrigQuery = query,
                Total = res?.Total ?? 0,
                IsValid = res?.IsValid ?? false,
                ElasticResults = res,
                ElapsedTime = sw.Elapsed
            };
            return search;
        }

    }
}
