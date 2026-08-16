import {createHttpClient} from "../../../shared/api/httpClient";

export const axios = createHttpClient(process.env.REACT_APP_BILLING_API_URL);
