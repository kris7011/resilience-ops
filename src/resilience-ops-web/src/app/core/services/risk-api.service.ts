import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
    CreateRiskRequest,
    Risk,
} from '../models/risk';

@Injectable({
    providedIn: 'root',
})
export class RiskApiService {
    private readonly httpClient = inject(HttpClient);

    private readonly apiUrl =
        'http://localhost:5014/api/risks';

    getRisks(): Observable<Risk[]> {
        return this.httpClient.get<Risk[]>(this.apiUrl);
    }

    createRisk(
        request: CreateRiskRequest,
    ): Observable<Risk> {
        return this.httpClient.post<Risk>(
            this.apiUrl,
            request,
        );
    }
}