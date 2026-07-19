export type MitigationStatus =
    | 'Planned'
    | 'InProgress'
    | 'Completed';

export interface Mitigation {
    id: string;
    riskId: string;
    description: string;
    owner: string;
    dueDateUtc: string;
    status: MitigationStatus;
    createdUtc: string;
}

export interface CreateMitigationRequest {
    description: string;
    owner: string;
    dueDateUtc: string;
}