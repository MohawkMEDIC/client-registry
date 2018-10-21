﻿
 -- @FUNCTION
-- GET DATABASE SCHEMA VERSION
--
-- RETURNS: THE MAJOR, MINOR AND RELEASE NUMBER OF THE DATABASE SCHEMA
CREATE OR REPLACE FUNCTION GET_SCH_VER() RETURNS VARCHAR AS
$$
BEGIN
	RETURN '1.2.0.0';
END;
$$ LANGUAGE plpgsql;

-- @PROCEDURE
-- PUSH A QUERY RESULT INTO A RESULT SET
CREATE OR REPLACE FUNCTION PUSH_QRY_RSLTS
(
	QRY_ID_IN	IN VARCHAR(128),
	RSLT_ENT	IN VARCHAR
) RETURNS VOID AS 
$$
DECLARE
	RSLT_ENT_ID_ARR DECIMAL[][];
BEGIN

	
	RSLT_ENT_ID_ARR := RSLT_ENT;
	FOR RSLT IN ARRAY_LOWER(RSLT_ENT_ID_ARR, 1) + 0 .. ARRAY_UPPER(RSLT_ENT_ID_ARR, 1) LOOP

		INSERT INTO QRY_RSLT_TBL (ENT_ID, QRY_ID, VRSN_ID)
			VALUES (RSLT_ENT_ID_ARR[RSLT][1], QRY_ID_IN, RSLT_ENT_ID_ARR[RSLT][2]);

	END LOOP;

	RETURN;
END
$$
LANGUAGE PLPGSQL;

-- 
-- CREATE VIEWS THAT WILL LOAD THE EFFECTIVE OBSOLETE TIMES
--
CREATE OR REPLACE VIEW ADDR_CMP_EFFT_VW AS
	SELECT ADDR_CMP_ID, ADDR_CMP_CLS, ADDR_CDTBL.ADDR_VALUE AS ADDR_CMP_VALUE, ADDR_SET_ID, EFFT_VRSN.CRT_UTC AS EFFT_UTC, OBSLT_VRSN.CRT_UTC AS OBSLT_UTC
		FROM ADDR_CMP_TBL INNER JOIN ADDR_CDTBL ON (ADDR_CMP_TBL.ADDR_CMP_VALUE = ADDR_CDTBL.ADDR_ID)
		LEFT JOIN PSN_ADDR_SET_TBL USING (ADDR_SET_ID)
		LEFT JOIN PSN_VRSN_TBL AS EFFT_VRSN ON (PSN_ADDR_SET_TBL.EFFT_VRSN_ID = EFFT_VRSN.PSN_VRSN_ID)
		LEFT JOIN PSN_VRSN_TBL AS OBSLT_VRSN ON (PSN_ADDR_SET_TBL.OBSLT_VRSN_ID = OBSLT_VRSN.PSN_VRSN_ID);

CREATE OR REPLACE VIEW PSN_ADDR_SET_EFFT_VW AS
	SELECT PSN_ADDR_SET_TBL.*, ADDR_CMP_VW.ADDR_CMP_ID, ADDR_CMP_VW.ADDR_CMP_CLS, ADDR_CMP_VW.ADDR_CMP_VALUE, EFFT_VRSN.CRT_UTC AS EFFT_UTC, OBSLT_VRSN.CRT_UTC AS OBSLT_UTC 
	FROM PSN_ADDR_SET_TBL INNER JOIN ADDR_CMP_VW USING (ADDR_SET_ID)
	INNER JOIN PSN_VRSN_TBL AS EFFT_VRSN ON (PSN_ADDR_SET_TBL.EFFT_VRSN_ID = EFFT_VRSN.PSN_VRSN_ID)
	LEFT JOIN PSN_VRSN_TBL AS OBSLT_VRSN ON (PSN_ADDR_SET_TBL.OBSLT_VRSN_ID = OBSLT_VRSN.PSN_VRSN_ID);

CREATE OR REPLACE VIEW NAME_CMP_EFFT_VW AS
	SELECT NAME_CMP_ID, NAME_CMP_CLS, NAME_CDTBL.NAME_VALUE AS NAME_CMP_VALUE, NAME_SET_ID, NAME_CDTBL.NAME_SOUNDEX, EFFT_VRSN.CRT_UTC AS EFFT_UTC, OBSLT_VRSN.CRT_UTC AS OBSLT_UTC
		FROM NAME_CMP_TBL INNER JOIN NAME_CDTBL ON (NAME_CMP_TBL.NAME_CMP_VALUE = NAME_CDTBL.NAME_ID)
		LEFT JOIN PSN_NAME_SET_TBL USING (NAME_SET_ID)
		LEFT JOIN PSN_VRSN_TBL AS EFFT_VRSN ON (PSN_NAME_SET_TBL.EFFT_VRSN_ID = EFFT_VRSN.PSN_VRSN_ID)
		LEFT JOIN PSN_VRSN_TBL AS OBSLT_VRSN ON (PSN_NAME_SET_TBL.OBSLT_VRSN_ID = OBSLT_VRSN.PSN_VRSN_ID);

CREATE OR REPLACE VIEW PSN_ALT_ID_EFFT_VW AS
	SELECT PSN_ALT_ID_TBL.*, EFFT_VRSN.CRT_UTC AS EFFT_UTC, OBSLT_VRSN.CRT_UTC AS OBSLT_UTC
	FROM PSN_ALT_ID_TBL INNER JOIN PSN_VRSN_TBL AS EFFT_VRSN ON (EFFT_VRSN.PSN_VRSN_ID = PSN_ALT_ID_TBL.EFFT_VRSN_ID)
	LEFT JOIN PSN_VRSN_TBL AS OBSLT_VRSN ON (OBSLT_VRSN.PSN_VRSN_ID = PSN_ALT_ID_TBL.OBSLT_VRSN_ID);

	
-- @FUNCTION GET PERSON ADDRESS SETS
CREATE OR REPLACE FUNCTION GET_PSN_ADDR_SETS_EFFT
(
	PSN_ID_IN		IN DECIMAL(20,0),
	PSN_VRSN_ID_IN		IN DECIMAL(20,0)
) RETURNS SETOF PSN_ADDR_SET_EFFT_VW
AS
$$
BEGIN
	RETURN QUERY SELECT * FROM PSN_ADDR_SET_EFFT_VW WHERE PSN_ID = PSN_ID_IN AND PSN_VRSN_ID_IN BETWEEN EFFT_VRSN_ID AND COALESCE(OBSLT_VRSN_ID, 9223372036854775807) - 1;
END;
$$ LANGUAGE plpgsql;


-- @FUNCTION
-- GET A NAME SET
--
-- RETURNS: A SET OF NAME_CMP_TBL RECORDS REPRESENTING THE SET
CREATE OR REPLACE FUNCTION GET_NAME_SET_EFFT
(
	NAME_SET_ID_IN		IN DECIMAL(20)
) RETURNS SETOF NAME_CMP_EFFT_VW
AS
$$
BEGIN
	RETURN QUERY SELECT * FROM NAME_CMP_EFFT_VW WHERE NAME_SET_ID = NAME_SET_ID_IN ORDER BY NAME_CMP_ID;
END
$$ LANGUAGE plpgsql;

-- @FUNCTION
-- GET THE CONTENTS OF AN ADDRESS SET
CREATE OR REPLACE FUNCTION GET_ADDR_SET_EFFT
(
	ADDR_SET_ID_IN		IN DECIMAL(20)
) RETURNS SETOF ADDR_CMP_EFFT_VW
AS 
$$
BEGIN
	RETURN QUERY SELECT * FROM ADDR_CMP_EFFT_VW WHERE ADDR_SET_ID = ADDR_SET_ID_IN ORDER BY ADDR_CMP_ID;
END
$$ LANGUAGE plpgsql;

-- @FUNCTION
-- GET ALTERNATE IDENTIFIERS 
CREATE OR REPLACE FUNCTION GET_PSN_ALT_ID_EFFT
(
	PSN_ID_IN		IN DECIMAL(20,0),
	PSN_VRSN_ID_IN		IN DECIMAL(20,0)
) RETURNS SETOF PSN_ALT_ID_EFFT_VW
AS
$$
BEGIN
	RETURN QUERY SELECT * FROM PSN_ALT_ID_EFFT_VW WHERE PSN_ID = PSN_ID_IN AND PSN_VRSN_ID_IN BETWEEN EFFT_VRSN_ID AND COALESCE(OBSLT_VRSN_ID, 9223372036854775807) - 1;
END
$$ LANGUAGE plpgsql;


ALTER TABLE psn_lang_tbl DROP CONSTRAINT ck_psn_lang_mode_cs;
ALTER TABLE psn_lang_tbl ADD CONSTRAINT ck_psn_lang_mode_cs CHECK (mode_cs >= 0 AND mode_cs <= 8);