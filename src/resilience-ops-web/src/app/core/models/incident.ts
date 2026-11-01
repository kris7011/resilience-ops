export const incidentStatuses = [
    'Reported',
    'Investigating',
    'Resolved',
    'Closed',
] as const;

export type IncidentStatus =
    (typeof incidentStatuses)[number];

export interface IncidentHistoryItem {
    id: string;
    incidentId: string;
    sequence: number;
    previousStatus: IncidentStatus | null;
    newStatus: IncidentStatus;
    occurredUtc: string;
}

export interface Incident {
    id: string;
    riskId: string;
    title: string;
    description: string;
    responsiblePerson: string;
    status: IncidentStatus;
    mitigationIds: string[];
    reportedUtc: string;
    investigatingUtc: string | null;
    resolvedUtc: string | null;
    closedUtc: string | null;
    updatedUtc: string;
    history: IncidentHistoryItem[];
}

export interface CreateIncidentRequest {
    title: string;
    description: string;
    responsiblePerson: string;
    mitigationIds: string[];
}
