export const riskSeverities = [
    'Low',
    'Medium',
    'High',
    'Critical',
] as const;

export type RiskSeverity = (typeof riskSeverities)[number];

export type RiskStatus = 'Open' | 'Mitigating' | 'Closed';

export interface Risk {
    id: string;
    title: string;
    description: string;
    severity: RiskSeverity;
    status: RiskStatus;
    createdUtc: string;
}

export interface CreateRiskRequest {
    title: string;
    description: string;
    severity: RiskSeverity;
}