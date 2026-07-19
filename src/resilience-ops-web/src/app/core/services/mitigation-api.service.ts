import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
    CreateMitigationRequest,
    Mitigation,
} from '../models/mitigation';

@Injectable({
    providedIn: 'root',
})
export class MitigationApiService {
    private readonly httpClient =
        inject(HttpClient);

    private readonly risksApiUrl =
        'http://localhost:5014/api/risks';

    getMitigations(
        riskId: string,
    ): Observable<Mitigation[]> {
        return this.httpClient.get<Mitigation[]>(
            `${this.risksApiUrl}/${riskId}/mitigations`,
        );
    }

    createMitigation(
        riskId: string,
        request: CreateMitigationRequest,
    ): Observable<Mitigation> {
        return this.httpClient.post<Mitigation>(
            `${this.risksApiUrl}/${riskId}/mitigations`,
            request,
        );
    }
}