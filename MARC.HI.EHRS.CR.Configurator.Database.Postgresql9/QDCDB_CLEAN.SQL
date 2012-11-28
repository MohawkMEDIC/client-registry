/* 
 * POSTGRESHR - MARC-HI SHARED SCHEMA - QUICK AND DIRTY CODE RESOLVER
 * VERSION: 3.0
 * AUTHOR: JUSTIN FYFE
 * DATE: APRIL 22, 2011
 * FILES:
 *	QDCODEDB.SQL		- SQL CODE TO CREATE TABLES, AND INDICIES FOR SIMEPL CODE DATABASE
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
 *
 */

-- @FUNCTION
 -- LOOKUP CODE
 DROP FUNCTION IF EXISTS QDCDB_LOOKUP_CD (
	OID_IN		IN VARCHAR(48),
	MNEMONIC_IN	IN VARCHAR(10)
 );

 -- @FUNCTION
 -- LOOKUP CODE
 DROP FUNCTION IF EXISTS QDCDB_GET_CD (
	OID_IN		IN VARCHAR(48),
	MNEMONIC_IN	IN VARCHAR(10)
 ) ;
 
 -- @FUNCTION
 -- REGISTER CODE
 DROP FUNCTION IF EXISTS QDCDB_REG_CD (
	MNEMONIC_IN	IN VARCHAR(10),
	OID_IN		IN VARCHAR(48),
	DISPLAY_IN	IN VARCHAR,
	PRNT_IN		IN NUMERIC(20,0)
 );

 -- @FUNCTION
 -- GET CHILD CODES
 DROP FUNCTION IF EXISTS GET_CHILD_CD (
	CNCPT_ID_IN	IN NUMERIC(20,0)
 );

 -- @FUNCTION
 -- REGISTER A FUNCTION MAP
 DROP FUNCTION IF EXISTS QDCDB_REG_MAP (
	CNCPT_ID_IN	IN NUMERIC(20,0),
	CNCPT_MAP_IN	IN NUMERIC(20,0)
 ) ;

 -- @FUNCTION
 -- LOOKUP A MAP
 DROP FUNCTION IF EXISTS QDCDB_MAP_CD (
	OID_IN		IN VARCHAR(48),
	MNEMONIC_IN	IN VARCHAR(10),
	TARG_OID_IN	IN VARCHAR(48)
 ) ;
 
 DROP SEQUENCE QDCDB_CNCPT_SEQ CASCADE;
 DROP TABLE QDCDB_CNCPT_CDTBL CASCADE;
 DROP TABLE QDCDB_CNCPT_MAP_TBL CASCADE;
 