import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
    CreateIncidentRequest,
    Incident,
    IncidentHistoryItem,
    IncidentStatus,
} from '../models/incident';

@Injectable({
    providedIn: 'root',
})
export class IncidentApiService {
    private readonly httpClient =
        inject(HttpClient);

    private readonly risksApiUrl =
        'http://localhost:5014/api/risks';

    getIncidents(
        riskId: string,
    ): Observable<Incident[]> {
        return this.httpClient.get<Incident[]>(
            `${this.risksApiUrl}/${riskId}/incidents`,
        );
    }

    getIncident(
        riskId: string,
        incidentId: string,
    ): Observable<Incident> {
        return this.httpClient.get<Incident>(
            `${this.risksApiUrl}/${riskId}/incidents/${incidentId}`,
        );
    }

    createIncident(
        riskId: string,
        request: CreateIncidentRequest,
    ): Observable<Incident> {
        return this.httpClient.post<Incident>(
            `${this.risksApiUrl}/${riskId}/incidents`,
            request,
        );
    }

    updateIncidentStatus(
        riskId: string,
        incidentId: string,
        status: IncidentStatus,
    ): Observable<Incident> {
        return this.httpClient.patch<Incident>(
            `${this.risksApiUrl}/${riskId}/incidents/${incidentId}/status`,
            {
                status,
            },
        );
    }

    getIncidentHistory(
        riskId: string,
        incidentId: string,
    ): Observable<IncidentHistoryItem[]> {
        return this.httpClient.get<
            IncidentHistoryItem[]
        >(
            `${this.risksApiUrl}/${riskId}/incidents/${incidentId}/history`,
        );
    }
}
