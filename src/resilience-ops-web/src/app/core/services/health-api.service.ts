import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { HealthResponse } from '../models/health-response';

@Injectable({
    providedIn: 'root',
})
export class HealthApiService {
    private readonly httpClient = inject(HttpClient);

    private readonly apiUrl = 'http://localhost:5014/api/health';

    getHealth(): Observable<HealthResponse> {
        return this.httpClient.get<HealthResponse>(this.apiUrl);
    }
}