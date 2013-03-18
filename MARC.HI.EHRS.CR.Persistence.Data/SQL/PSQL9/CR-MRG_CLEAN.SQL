/**
 * POSTGRECR - MARC-HI CLIENT REGISTRY MERGE FUNCTIONS FOR POSTGRESQL
 * VERSION: 2.0
 * AUTHOR: JUSTIN FYFE
 * DATE: MARCH 17, 2013
 * DESCRIPTION:
 *	THIS FILE CLEANS AND CREATES THE PORTIONS OF THE CLIENT REGISTRY SCHEMA NECESSARY FOR 
 *	PERFORMING MERGES
 *
 * LICENSE:
 * 	Licensed under the Apache License, Version 2.0 (the "License");
 * 	you may not use this file except in compliance with the License.
 * 	You may obtain a copy of the License at
 *
 *     		http://www.apache.org/licenses/LICENSE-2.0
 *
 * 	Unless required by applicable law or agreed to in writing, software
 * 	distributed under the License is distributed on an "AS IS" BASIS,
 * 	WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * 	See the License for the specific language governing permissions and
 * 	limitations under the License.
 */

-- DROP HSR MERGE CANDIDATE TABLE
DROP TABLE HSR_MRG_CAND_TBL CASCADE;


-- @FUNCTION
-- CREATE A MERGE CANDIDATE RECORD
DROP FUNCTION IF EXISTS CRT_MRG_CAND
(
NUMERIC,
NUMERIC
) 
;

-- @FUNCTION
-- MARK A MERGE A COMPLETE
DROP FUNCTION IF EXISTS MRG_CAND
(
NUMERIC,
NUMERIC
) 
;

-- @FUNCTION
-- IGNORE A MERGE
DROP FUNCTION IF EXISTS MRG_IGNR
(
NUMERIC
) 
;

-- @FUNCTION
-- GET OUTSTANDING MERGES
DROP FUNCTION IF EXISTS GET_OUTSD_MRG
(
) 
;

-- @FUNCTION
-- GET MERGE CANDIDATES
DROP FUNCTION IF EXISTS GET_MRG_CAND
(
NUMERIC
) 
;