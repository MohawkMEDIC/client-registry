--
-- Copyright 2012-2013 Mohawk College of Applied Arts and Technology
-- 
-- Licensed under the Apache License, Version 2.0 (the "License"); you 
-- may not use this file except in compliance with the License. You may 
-- obtain a copy of the License at 
-- 
-- http://www.apache.org/licenses/LICENSE-2.0 
-- 
-- Unless required by applicable law or agreed to in writing, software
-- distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
-- WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
-- License for the specific language governing permissions and limitations under 
-- the License.
-- 
-- User: fyfej
-- Date: 5-12-2012
--
DROP FUNCTION IF EXISTS REG_MSG
(
	MSG_ID_IN	IN VARCHAR(48), -- ID OF THE MESSAGE BEING REGISTERED
	MSG_BODY_IN	IN BYTEA, -- BODY OF THE MESSAGE
	MSG_RSP_IN	IN VARCHAR(48) -- NOT NULL
);

DROP FUNCTION IF EXISTS GET_MSG
(
	MSG_ID_IN	IN VARCHAR(48) -- THE ID OF THE MESSAGE FOR WHICH TO FETCH
);

DROP FUNCTION IF EXISTS GET_RSP_MSG
(
	MSG_ID_IN	IN VARCHAR(48) -- THE ID OF THE MESSAGE FOR WHICH TO FETCH
);

DROP FUNCTION IF EXISTS QRY_MSG_STATE
(
	MSG_ID_IN	IN VARCHAR(48)
);

DROP TABLE MSG_TBL CASCADE;
