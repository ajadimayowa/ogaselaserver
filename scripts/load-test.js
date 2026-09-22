import http from "k6/http";
import { check, sleep } from "k6";

// Basic load test for the two read endpoints most likely to sit on the hot path of the app's
// home/search experience: full-text listing search and a single listing's detail view. See
// LOAD_TEST_RESULTS.md at the repo root for how to run this and the results of the last run.
//
// Usage:
//   BASE_URL=http://localhost:5080 LISTING_ID=<a real published listing id> k6 run scripts/load-test.js

const BASE_URL = __ENV.BASE_URL || "http://localhost:5080";
const LISTING_ID = __ENV.LISTING_ID;
const SEARCH_QUERY = __ENV.SEARCH_QUERY || "iPhone";

if (!LISTING_ID) {
  throw new Error("LISTING_ID env var is required - pass the id of a real, published listing to load-test against.");
}

export const options = {
  scenarios: {
    search: {
      executor: "constant-vus",
      exec: "search",
      vus: 25,
      duration: "30s",
    },
    listingDetail: {
      executor: "constant-vus",
      exec: "listingDetail",
      vus: 25,
      duration: "30s",
    },
  },
  thresholds: {
    // The moderate-load p95 latency budget this load test exists to confirm.
    "http_req_duration{endpoint:search}": ["p(95)<800"],
    "http_req_duration{endpoint:listing_detail}": ["p(95)<800"],
    http_req_failed: ["rate<0.01"],
  },
};

export function search() {
  const res = http.get(`${BASE_URL}/api/v1/search?query=${encodeURIComponent(SEARCH_QUERY)}`, {
    tags: { endpoint: "search" },
  });
  check(res, { "search: status is 200": (r) => r.status === 200 });
  sleep(0.2);
}

export function listingDetail() {
  const res = http.get(`${BASE_URL}/api/v1/listings/${LISTING_ID}`, {
    tags: { endpoint: "listing_detail" },
  });
  check(res, { "listing detail: status is 200": (r) => r.status === 200 });
  sleep(0.2);
}
