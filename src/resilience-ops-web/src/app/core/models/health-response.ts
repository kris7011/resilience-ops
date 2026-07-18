export interface HealthResponse {
    status: string;
    service: string;
    environment: string;
    timestampUtc: string;
}