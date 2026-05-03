import { environment } from '../../../environments/environment';

export const API_CONFIG = {
  GATEWAY_URL: environment.gatewayUrl,
  
  // Gateway routes - Ocelot transforms these:
  // /gateway/auth/{everything} → /api/v1/{everything}
  // /gateway/catalog/{everything} → /api/v1/{everything}
  // /gateway/workflow/{everything} → /api/v1/{everything}
  // /gateway/admin/{everything} → /api/v1/{everything}
  // /gateway/notification/{everything} → /api/v1/{everything}
  
  AUTH_API: `${environment.gatewayUrl}/gateway/auth`,
  CATALOG_API: `${environment.gatewayUrl}/gateway/catalog`,
  WORKFLOW_API: `${environment.gatewayUrl}/gateway/workflow`,
  REPORTING_API: `${environment.gatewayUrl}/gateway/admin`,
  NOTIFICATION_API: `${environment.gatewayUrl}/gateway/notification`
} as const;

export type ApiEndpoint = keyof typeof API_CONFIG;
